using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Enums;
using BarrieraMoving.Server.Models;

namespace BarrieraMoving.Server.Services
{
    // Using Primary Constructor de C# 14
    public class TicketService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        public async Task<List<Ticket>> GetTicketsByUserIdAsync(string userId)
        {
            using var context = dbFactory.CreateDbContext();
            return await context.Tickets
                .Include(t => t.Category) // Importante para mostrar el nombre de la categoría
                .Include(t => t.Author)   // Por si necesitas mostrar el email del autor
                .Where(t => t.AuthorId == userId)
                .OrderByDescending(t => t.CreatedAt) // Los más nuevos primero
                .ToListAsync();
        }
        public async Task<List<Ticket>> GetMyWorkAndRequestsAsync(string userId)
        {
            using var context = dbFactory.CreateDbContext();
            return await context.Tickets
                .Include(t => t.Category) // Para mostrar el nombre de la categoría en la lista
                .Include(t => t.Author)   // Para mostrar el email del autor
                .Include(t => t.Technician) // Para mostrar el email del técnico asignado
                .Where(t => t.AuthorId == userId || t.TechnicianId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
        // --- MÓDULO 4: MÉTODOS DE CREACIÓN ---

        public async Task<List<Category>> GetCategoriesAsync()
        {
            using var context = dbFactory.CreateDbContext();
            return await context.Categories.ToListAsync();
        }

        public async Task CreateTicketAsync(Ticket ticket)
        {
            using var context = dbFactory.CreateDbContext();
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();
        }
        public async Task<List<Ticket>> GetTicketsForUserAsync(ClaimsPrincipal user)
        {
            using var context = dbFactory.CreateDbContext();

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var query = context.Tickets
                .Include(t => t.Category)
                .Include(t => t.Author)
                .Include(t => t.Technician)
                .AsQueryable();

            if (user.IsInRole(Roles.Admin))
            {
                // El admin ve todo
                return await query.ToListAsync();
            }
            else if (user.IsInRole(Roles.Technician))
            {
                return await query.Where(t => t.TechnicianId == userId || t.Status == Enums.TicketStatus.Open).ToListAsync();
            }
            // El empleado solo ve los tickets que él mismo creó
            return await query.Where(t => t.AuthorId == userId).ToListAsync();
        }
        public async Task<List<Ticket>> GetAllTicketsAsync()
        {
            using var context = dbFactory.CreateDbContext();
            return await context.Tickets
                .Include(t => t.Category)
                .Include(t => t.Author)
                .Include(t => t.Technician)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        // Método para actualizar el estado de un ticket, con lógica para registrar eventos importantes
        public async Task UpdateTicketStatusAsync(int ticketId, TicketStatus newStatus, string? newTechId = null, string? performerId = null)
        {
            using var context = dbFactory.CreateDbContext();

            var ticket = await context.Tickets
                .Include(t => t.Technician)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket != null)
            {
                string systemLog = "";
                var oldStatus = ticket.Status;
                var oldTechId = ticket.TechnicianId;

                // ESCENARIO 1: Solo cambio de Técnico (Asignación por Admin)
                if (!string.IsNullOrEmpty(newTechId) && newTechId != oldTechId)
                {
                    ticket.TechnicianId = newTechId;
                    // No cambiamos el status aquí, se mantiene el que tiene (Open)
                    systemLog = $"[SISTEMA] El Administrador ha asignado este ticket al equipo técnico.";
                }

                // ESCENARIO 2: Cambio de Estado (Técnico inicia o resuelve)
                if (newStatus != oldStatus)
                {
                    ticket.Status = newStatus;
                    systemLog = string.IsNullOrEmpty(systemLog)
                        ? $"[EVENTO] El estado cambió de {oldStatus} a {newStatus}."
                        : systemLog + $" El estado actual es {newStatus}.";
                }

                if (!string.IsNullOrEmpty(systemLog))
                {
                    var systemComment = new TicketComment
                    {
                        TicketId = ticketId,
                        UserId = performerId ?? (newTechId ?? ticket.AuthorId),
                        Content = systemLog,
                        CreatedAt = DateTime.UtcNow
                    };

                    context.TicketComments.Add(systemComment);
                    ticket.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                }
            }
        }

        public async Task<List<ApplicationUser>> GetUsersByRoleAsync(string roleName)
        {
            using var context = dbFactory.CreateDbContext();

            // Buscamos el ID del rol primero
            var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null) return new List<ApplicationUser>();

            // Buscamos los IDs de usuarios asociados a ese rol
            var userIds = await context.UserRoles
                .Where(ur => ur.RoleId == role.Id)
                .Select(ur => ur.UserId)
                .ToListAsync();

            // Retornamos la lista de objetos de usuario
            return await context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();
        }

        // Obtener un ticket específico con todos sus detalles y relaciones
        public async Task<Ticket?> GetTicketByIdAsync(int id)
        {
            using var context = dbFactory.CreateDbContext();
            return await context.Tickets
                .Include(t => t.Category)
                .Include(t => t.Author)
                .Include(t => t.Technician)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        // Listar todos los técnicos disponibles para asignar
        public async Task<List<ApplicationUser>> GetTechniciansAsync()
        {
            using var context = dbFactory.CreateDbContext();
            var techRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == Roles.Technician);
            if (techRole == null) return [];

            var techIds = await context.UserRoles
                .Where(ur => ur.RoleId == techRole.Id)
                .Select(ur => ur.UserId).ToListAsync();

            return await context.Users.Where(u => techIds.Contains(u.Id)).ToListAsync();
        }

        // Método específico para que el admin asigne un técnico a un ticket, con registro de evento
        public async Task AssignTicketAsync(int ticketId, string technicianId, string adminId)
        {
            using var context = dbFactory.CreateDbContext();
            var ticket = await context.Tickets
                .Include(t => t.Technician)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket != null)
            {
                ticket.TechnicianId = technicianId;
                ticket.Status = TicketStatus.InProgress;

                var systemMessage = new TicketComment
                {
                    TicketId = ticketId,
                    UserId = adminId,
                    Content = $"[SISTEMA] El Administrador ha asignado este ticket al equipo técnico.",
                    CreatedAt = DateTime.UtcNow
                };
                context.TicketComments.Add(systemMessage);

                await context.SaveChangesAsync();
            }
        }

             // --- MÓDULO 7: MÉTODOS DE REPORTES Y ESTADÍSTICAS ---
               public async Task<Dictionary<string, int>> GetTicketStatsAsync()
               {
           using var context = dbFactory.CreateDbContext();
         return new Dictionary<string, int>
                {
          ["Total"] = await context.Tickets.CountAsync(),
           ["Open"] = await context.Tickets.CountAsync(t => t.Status == TicketStatus.Open),
        ["InProgress"] = await context.Tickets.CountAsync(t => t.Status == TicketStatus.InProgress),
           ["Resolved"] = await context.Tickets.CountAsync(t => t.Status == TicketStatus.Resolved),
           ["Urgent"] = await context.Tickets.CountAsync(t => t.Priority == TicketPriority.Urgent)
                   };
               }
        public async Task<List<ApplicationUser>> GetAllUsersAsync()
        {
            using var context = dbFactory.CreateDbContext();
      return await context.Users.OrderBy(u => u.Email).ToListAsync();
        }

        // --- MÓDULO 9 - Comunicación y Cierre del Ciclo de Vida ---
        // Método para agregar un comentario a un ticket
        public async Task AddCommentAsync(TicketComment comment)
     {
        using var context = dbFactory.CreateDbContext();
            context.TicketComments.Add(comment);
         await context.SaveChangesAsync();
        }

        // Método para obtener los comentarios de un ticket específico
        public async Task<List<TicketComment>> GetCommentsAsync(int ticketId)
        {
  using var context = dbFactory.CreateDbContext();
 return await context.TicketComments
      .Include(c => c.User) // Incluir información del usuario que hizo el comentario
     .Where(c => c.TicketId == ticketId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
        }

        // Método para cerrar un ticket (cambiar su estado a Resuelto o Cerrado)
        public async Task CloseTicketAsync(int ticketId)
  {
    using var context = dbFactory.CreateDbContext();
        var ticket = await context.Tickets.FindAsync(ticketId);
       if (ticket != null)
    {
   ticket.Status = TicketStatus.Resolved; // O un estado 'Closed'
  ticket.UpdatedAt = DateTime.UtcNow;
 await context.SaveChangesAsync();
    }
        }

   // Método para generar un reporte en Excel con el resumen de tickets, usando ClosedXML
        public async Task<byte[]> GenerateExcelReportAsync()
        {
            using var context = dbFactory.CreateDbContext();
    // Traer todos los tickets con sus relaciones para mostrar información completa en el reporte
   var tickets = await context.Tickets
  .Include(t => t.Author)
         .Include(t => t.Technician)
      .Include(t => t.Category)
                .ToListAsync();
// Crear un nuevo libro de Excel
     using var workbook = new XLWorkbook();
      var worksheet = workbook.Worksheets.Add("Resumen de Tickets");

            // Encabezados
 var headers = new[] { "ID", "Titulo", "Categoría", "Prioridad", "Estado", "Creado", "Resuelto",
    "Tiempo (Días)", "Autor", "Técnico" };
            // Estilo para encabezados
            for (int i = 0; i < headers.Length; i++)
       {
worksheet.Cell(1, i + 1).Value = headers[i];
      worksheet.Cell(1, i + 1).Style.Font.Bold = true;
worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Llenar datos de los tickets
   int row = 2;
     foreach (var t in tickets)
        {
        worksheet.Cell(row, 1).Value = t.Id;
       worksheet.Cell(row, 2).Value = t.Title;
       worksheet.Cell(row, 3).Value = t.Category?.name ?? "N/A";
                worksheet.Cell(row, 4).Value = t.Priority.ToString();
 worksheet.Cell(row, 5).Value = t.Status.ToString();
                worksheet.Cell(row, 6).Value = t.CreatedAt;
        // Solo calcular el tiempo de resolución si el ticket está resuelto y tiene fecha de actualización
  if (t.Status == TicketStatus.Resolved && t.UpdatedAt.HasValue)
                {
           worksheet.Cell(row, 7).Value = t.UpdatedAt.Value;
       // Cálculo de tiempo de resolución (Diferencia entre fechas)
 var duration = t.UpdatedAt.Value - t.CreatedAt;
      worksheet.Cell(row, 8).Value = Math.Round(duration.TotalDays, 2);
      }
          worksheet.Cell(row, 9).Value = t.Author?.Email;
    worksheet.Cell(row, 10).Value = t.Technician?.Email ?? "No asignado";
   row++;
       }
            // Ajustar el ancho de las columnas para que se vea bien
     worksheet.Columns().AdjustToContents();
    // Guardar el libro en un MemoryStream y retornar el arreglo de bytes
 using var stream = new MemoryStream();
   workbook.SaveAs(stream);
            return stream.ToArray();
        }
  }
}
