using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApp.Models;
using RestaurantApp.Services;

namespace RestaurantApp.Controllers
{
    [ApiController]
    [Route("api/restaurants/{restaurantId}/menu")]
    public class MenuController : ControllerBase
    {
        private readonly IMenuService _menuService;

        public MenuController(IMenuService menuService)
        {
            _menuService = menuService;
        }

        /// <summary>Get the full menu for a restaurant.</summary>
        [HttpGet]
        public async Task<IActionResult> GetMenu(int restaurantId)
        {
            var items = await _menuService.GetMenuByRestaurant(restaurantId);
            return Ok(items);
        }

        /// <summary>Get menu items filtered by category.</summary>
        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetMenuByCategory(int restaurantId, string category)
        {
            var items = await _menuService.GetmenubyCategory(restaurantId, category);
            return Ok(items);
        }

        /// <summary>Get a single menu item by ID.</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMenuItem(int restaurantId, int id)
        {
            var item = await _menuService.GetMenuItemById(id);
            if (item == null || item.RestaurantId != restaurantId)
                return NotFound(new { message = $"Menu item {id} not found." });
            return Ok(item);
        }

        /// <summary>Add a new menu item. Admin or Manager only.</summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AddMenuItem(int restaurantId, [FromBody] MenuItemModel menuItem)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            menuItem.RestaurantId = restaurantId;
            var created = await _menuService.AddMenuItem(menuItem);
            return CreatedAtAction(nameof(GetMenuItem), new { restaurantId, id = created.Id }, created);
        }

        /// <summary>Update a menu item. Admin or Manager only.</summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateMenuItem(int restaurantId, int id, [FromBody] MenuItemModel menuItem)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existing = await _menuService.GetMenuItemById(id);
            if (existing == null || existing.RestaurantId != restaurantId)
                return NotFound(new { message = $"Menu item {id} not found." });

            var updated = await _menuService.UpdateMenuItem(id, menuItem);
            return Ok(updated);
        }

        /// <summary>Soft-delete a menu item. Admin or Manager only.</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteMenuItem(int restaurantId, int id)
        {
            var existing = await _menuService.GetMenuItemById(id);
            if (existing == null || existing.RestaurantId != restaurantId)
                return NotFound(new { message = $"Menu item {id} not found." });

            await _menuService.DeleteMenuItem(id);
            return NoContent();
        }

        /// <summary>Toggle item availability. Admin, Manager, or Operator.</summary>
        [HttpPatch("{id}/availability")]
        [Authorize(Roles = "Admin,Manager,Operator")]
        public async Task<IActionResult> SetAvailability(int restaurantId, int id, [FromBody] SetAvailabilityDto dto)
        {
            var existing = await _menuService.GetMenuItemById(id);
            if (existing == null || existing.RestaurantId != restaurantId)
                return NotFound(new { message = $"Menu item {id} not found." });

            await _menuService.SetAvailability(id, dto.IsAvailable);
            return Ok(new { message = $"Availability set to {dto.IsAvailable}." });
        }

        /// <summary>Update the price of a menu item. Admin or Manager only.</summary>
        [HttpPatch("{id}/price")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdatePrice(int restaurantId, int id, [FromBody] UpdatePriceDto dto)
        {
            var existing = await _menuService.GetMenuItemById(id);
            if (existing == null || existing.RestaurantId != restaurantId)
                return NotFound(new { message = $"Menu item {id} not found." });

            await _menuService.UpdatePrice(id, dto.Price);
            return Ok(new { message = "Price updated." });
        }

        /// <summary>Apply a discount percentage to a menu item. Admin or Manager only.</summary>
        [HttpPatch("{id}/discount")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ApplyDiscount(int restaurantId, int id, [FromBody] ApplyDiscountDto dto)
        {
            var existing = await _menuService.GetMenuItemById(id);
            if (existing == null || existing.RestaurantId != restaurantId)
                return NotFound(new { message = $"Menu item {id} not found." });

            await _menuService.ApplyDiscount(id, dto.DiscountPercentage);
            return Ok(new { message = $"Discount of {dto.DiscountPercentage}% applied." });
        }
    }

    public record SetAvailabilityDto(bool IsAvailable);
    public record UpdatePriceDto(decimal Price);
    public record ApplyDiscountDto(decimal DiscountPercentage);
}
