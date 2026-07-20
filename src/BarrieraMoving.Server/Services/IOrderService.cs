using System.Security.Claims;
using BarrieraMoving.Server.Data;
using BarrieraMoving.Server.Models;
using BarrieraMoving.Shared.Enums;

namespace BarrieraMoving.Server.Services;

public interface IOrderService
{
    Task<List<Order>> GetMyWorkAndRequestsAsync(string userId);
    Task<List<Category>> GetCategoriesAsync();
    Task CreateOrderAsync(Order order);
    Task<List<Order>> GetOrdersForUserAsync(ClaimsPrincipal user);
    Task<List<Order>> GetAllOrdersAsync();
    Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string? newDriverId = null,
        string? performerId = null, bool bypassValidation = false);
    Task<List<ApplicationUser>> GetUsersByRoleAsync(string roleName);
    Task<Order?> GetOrderByIdAsync(int id);
    Task<List<ApplicationUser>> GetDriversAsync();
    Task<List<ApplicationUser>> GetAllUsersAsync();
    Task AddMessageAsync(Message message);
    Task<List<Message>> GetMessagesAsync(int orderId, int take = 50, int? beforeId = null, int? afterId = null);
    Task<Message?> GetMessageWithOrderAsync(int messageId);
    Task<Message?> FindMessageByIdempotencyKeyAsync(string idempotencyKey);
}
