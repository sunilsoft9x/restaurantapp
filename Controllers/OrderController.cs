using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApp.DTOs;
using RestaurantApp.Services;

namespace RestaurantApp.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var claimValue = User.FindFirstValue("id");
            return !string.IsNullOrWhiteSpace(claimValue) && int.TryParse(claimValue, out userId);
        }

        /// <summary>Create a new order. Authenticated users only. UserId is taken from the JWT token.</summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid or missing user claim." });

            var order = await _orderService.CreateOrderAsync(userId, dto);
            return StatusCode(201, order);
        }

        /// <summary>Get the current user's own orders.</summary>
        [HttpGet("my")]
        public async Task<IActionResult> GetMyOrders()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid or missing user claim." });

            var orders = await _orderService.GetOrdersByUserAsync(userId);
            return Ok(orders);
        }

        /// <summary>Get a specific order by ID. Users can only see their own; Admins see all.</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid or missing user claim." });

            var isAdmin = User.IsInRole("Admin") || User.IsInRole("Manager");

            var order = await _orderService.GetOrderByIdAsync(id);

            if (!isAdmin && order.UserId != userId)
                return StatusCode(403, new { message = "You do not have access to this order." });

            return Ok(order);
        }

        /// <summary>Get all orders for a restaurant. Admin, Manager, or Operator only.</summary>
        [HttpGet("restaurant/{restaurantId}")]
        [Authorize(Roles = "Admin,Manager,Operator")]
        public async Task<IActionResult> GetOrdersByRestaurant(int restaurantId)
        {
            var orders = await _orderService.GetOrdersByRestaurantAsync(restaurantId);
            return Ok(orders);
        }

        /// <summary>Update the order status. Admin, Manager, or Operator only.</summary>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin,Manager,Operator")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _orderService.UpdateOrderStatusAsync(id, dto.Status);
            if (!result) return NotFound(new { message = $"Order {id} not found." });
            return Ok(new { message = "Order status updated." });
        }

        /// <summary>Update the delivery status. Admin, Manager, or Operator only.</summary>
        [HttpPatch("{id}/delivery-status")]
        [Authorize(Roles = "Admin,Manager,Operator")]
        public async Task<IActionResult> UpdateDeliveryStatus(int id, [FromBody] UpdateDeliveryStatusDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _orderService.UpdateDeliveryStatusAsync(id, dto.DeliveryStatus);
            if (!result) return NotFound(new { message = $"Order {id} not found." });
            return Ok(new { message = "Delivery status updated." });
        }

        /// <summary>Update the payment status. Admin or Manager only.</summary>
        [HttpPatch("{id}/payment-status")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdatePaymentStatus(int id, [FromBody] UpdatePaymentStatusDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _orderService.UpdatePaymentStatusAsync(id, dto.PaymentStatus);
            if (!result) return NotFound(new { message = $"Order {id} not found." });
            return Ok(new { message = "Payment status updated." });
        }

        /// <summary>Cancel an order. Users can only cancel their own Pending/Confirmed orders.</summary>
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid or missing user claim." });

            var result = await _orderService.CancelOrderAsync(id, userId);
            if (!result) return BadRequest(new { message = "Order cannot be cancelled. It may not exist, already be cancelled, or is past the cancellable stage." });
            return Ok(new { message = "Order cancelled successfully." });
        }
    }

    public record UpdateOrderStatusDto(string Status);
    public record UpdateDeliveryStatusDto(string DeliveryStatus);
    public record UpdatePaymentStatusDto(string PaymentStatus);
}
