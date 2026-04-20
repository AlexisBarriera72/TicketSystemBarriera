using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketSystemBarriera.Data;
using TicketSystemBarriera.Enums;
using TicketSystemBarriera.Models;

namespace TicketSystemBarriera.Services
{
    // Using Primary Constructor de C# 14
    public class TicketService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
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

        public async Task UpdateTicketAsync(Ticket ticket)
        {
            using var context = dbFactory.CreateDbContext();
            ticket.UpdatedAt = DateTime.UtcNow;
            context.Entry(ticket).State = EntityState.Modified;
            await context.SaveChangesAsync();
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

        // Actualizar el ticket (Asignación o Cambio de Estado)
        public async Task UpdateTicketStatusAsync(int ticketId, TicketStatus newStatus, string? technicianId = null)
        {
            using var context = dbFactory.CreateDbContext();
            var ticket = await context.Tickets.FindAsync(ticketId);
            if (ticket != null)
            {
                ticket.Status = newStatus;
                if (!string.IsNullOrEmpty(technicianId)) ticket.TechnicianId = technicianId;
                ticket.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync();
            }
        }
    }
}
