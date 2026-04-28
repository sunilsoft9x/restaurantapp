using RestaurantApp.Data;
using RestaurantApp.DTOs;
using RestaurantApp.Exceptions;
using RestaurantApp.Models;
using Microsoft.EntityFrameworkCore;

namespace RestaurantApp.Services
{
    public static class OrderStatus
    {
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";
        public const string Preparing = "Preparing";
        public const string Ready = "Ready";
        public const string OutForDelivery = "OutForDelivery";
        public const string Delivered = "Delivered";
        public const string Cancelled = "Cancelled";

        private static readonly Dictionary<string, string[]> ValidTransitions = new()
        {
            [Pending]        = [Confirmed, Cancelled],
            [Confirmed]      = [Preparing, Cancelled],
            [Preparing]      = [Ready],
            [Ready]          = [OutForDelivery],
            [OutForDelivery] = [Delivered],
            [Delivered]      = [],
            [Cancelled]      = []
        };

        public static bool IsValidTransition(string current, string next) =>
            ValidTransitions.TryGetValue(current, out var allowed) && allowed.Contains(next);
    }

    public static class PaymentStatus
    {
        public const string Unpaid = "Unpaid";
        public const string Paid = "Paid";
        public const string Refunded = "Refunded";
    }

    public static class DeliveryStatus
    {
        public const string Pending = "Pending";
        public const string OutForDelivery = "OutForDelivery";
        public const string Delivered = "Delivered";
    }

    public class OrderService : IOrderService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public OrderService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<OrderResponseDto> CreateOrderAsync(int userId, OrderCreateDto dto)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == dto.RestaurantId && !r.IsDeleted)
                ?? throw new NotFoundException("Restaurant", dto.RestaurantId);

            var order = new OrderModel
            {
                UserId = userId,
                RestaurantId = dto.RestaurantId,
                DeliveryAddress = dto.DeliveryAddress,
                ContactNumber = dto.ContactNumber,
                Status = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Unpaid,
                DeliveryStatus = DeliveryStatus.Pending,
                PaymentMethod = dto.PaymentMethod
            };

            decimal subtotal = 0;
            foreach (var item in dto.OrderItems)
            {
                var menuItem = await _context.MenuItems
                    .FirstOrDefaultAsync(m => m.Id == item.MenuItemId && !m.IsDeleted)
                    ?? throw new NotFoundException($"Menu item with ID {item.MenuItemId} not found.");

                if (!menuItem.IsAvailable)
                    throw new AppValidationException($"Menu item '{menuItem.Name}' is currently unavailable.");

                if (menuItem.RestaurantId != dto.RestaurantId)
                    throw new AppValidationException($"Menu item '{menuItem.Name}' does not belong to this restaurant.");

                subtotal += menuItem.Price * item.Quantity;

                order.OrderItems.Add(new OrderItemModel
                {
                    MenuItemId = item.MenuItemId,
                    MenuItemName = menuItem.Name,
                    Quantity = item.Quantity,
                    UnitPrice = menuItem.Price,
                    SpecialInstructions = item.SpecialInstructions
                });
            }

            var gstRate = decimal.TryParse(_configuration["Tax:GstRate"], out var rate) ? rate : 0.05m;
            order.TotalAmount = subtotal;
            order.GST = Math.Round(subtotal * gstRate, 2);
            order.DiscountAmount = 0;
            order.DeliveryFee = 0;
            order.FinalBillAmount = order.TotalAmount + order.GST - order.DiscountAmount + order.DeliveryFee;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Reload restaurant for mapping
            await _context.Entry(order).Reference(o => o.Restaurant).LoadAsync();

            return MapToOrderResponseDto(order);
        }

        private static OrderResponseDto MapToOrderResponseDto(OrderModel order)
        {
            return new OrderResponseDto
            {
                Id = order.Id,
                UserId = order.UserId,
                RestaurantId = order.RestaurantId,
                RestaurantName = order.Restaurant?.Name ?? string.Empty,
                OrderItems = order.OrderItems.Select(oi => new OrderItemResponseDto
                {
                    MenuItemId = oi.MenuItemId,
                    ItemName = oi.MenuItemName,
                    UnitPrice = oi.UnitPrice,
                    Quantity = oi.Quantity,
                    TotalPrice = oi.UnitPrice * oi.Quantity,
                    SpecialInstructions = oi.SpecialInstructions
                }).ToList(),
                OrderStatus = order.Status,
                DeliveryStatus = order.DeliveryStatus,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                DeliveryAddress = order.DeliveryAddress,
                DeliveryFee = order.DeliveryFee,
                ContactNumber = order.ContactNumber,
                Subtotal = order.TotalAmount,
                TaxAmount = order.GST,
                CouponCode = order.CouponCode,
                DiscountAmount = order.DiscountAmount,
                TotalAmount = order.FinalBillAmount,
                CreatedAt = order.CreatedAt
            };
        }

        public async Task<List<OrderResponseDto>> GetOrdersByUserAsync(int userId)
        {
            var orders = await _context.Orders
                .Include(o => o.Restaurant)
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
            return orders.Select(MapToOrderResponseDto).ToList();
        }

        public async Task<List<OrderResponseDto>> GetOrdersByRestaurantAsync(int restaurantId)
        {
            var orders = await _context.Orders
                .Include(o => o.Restaurant)
                .Include(o => o.OrderItems)
                .Where(o => o.RestaurantId == restaurantId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
            return orders.Select(MapToOrderResponseDto).ToList();
        }

        public async Task<OrderResponseDto> GetOrderByIdAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Restaurant)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId)
                ?? throw new NotFoundException("Order", orderId);

            return MapToOrderResponseDto(order);
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            if (!OrderStatus.IsValidTransition(order.Status, newStatus))
                throw new AppValidationException($"Cannot transition order from '{order.Status}' to '{newStatus}'.");

            order.Status = newStatus;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateDeliveryStatusAsync(int orderId, string newDeliveryStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            order.DeliveryStatus = newDeliveryStatus;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdatePaymentStatusAsync(int orderId, string newPaymentStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            order.PaymentStatus = newPaymentStatus;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetPaymentMethodAsync(int orderId, string paymentMethod)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            order.PaymentMethod = paymentMethod;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelOrderAsync(int orderId, int userId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null || order.UserId != userId) return false;

            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
                return false;

            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
