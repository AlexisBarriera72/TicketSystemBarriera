using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketSystemBarriera.Data;
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
        }
    }
