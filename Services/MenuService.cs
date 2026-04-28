using Microsoft.EntityFrameworkCore;
using RestaurantApp.Data;
using RestaurantApp.Models;

namespace RestaurantApp.Services
{
    public class MenuService : IMenuService
    {
        private readonly AppDbContext _context;

        public MenuService(AppDbContext context)
        {
            _context = context;
        }

        // Add a new menu item
        public async Task<MenuItemModel> AddMenuItem(MenuItemModel menuItem)
        {
            _context.MenuItems.Add(menuItem);
            await _context.SaveChangesAsync();
            return menuItem;
        }

        // Get all menu items for a restaurant
        public async Task<List<MenuItemModel>> GetMenuByRestaurant(int restaurantId)
        {
            return await _context.MenuItems
                .Where(m => m.RestaurantId == restaurantId && !m.IsDeleted)
                .ToListAsync();
        }

        // Get a single menu item by ID
        public async Task<MenuItemModel?> GetMenuItemById(int id)
        {
            return await _context.MenuItems
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
        }

        // Update a menu item
        public async Task<MenuItemModel> UpdateMenuItem(int menuItemId, MenuItemModel updatedMenuItem)
        {
            var menuItem = await _context.MenuItems.FirstOrDefaultAsync(m => m.Id == menuItemId && !m.IsDeleted);
            if (menuItem == null)
            {
                throw new Exception($"Menu item with ID {menuItemId} not found");
            }

            menuItem.Name = updatedMenuItem.Name;
            menuItem.Description = updatedMenuItem.Description;
            menuItem.Price = updatedMenuItem.Price;
            menuItem.Category = updatedMenuItem.Category;
            menuItem.IsAvailable = updatedMenuItem.IsAvailable;

            await _context.SaveChangesAsync();
            return menuItem;
        }

        // Soft delete a menu item
        public async Task<bool> DeleteMenuItem(int menuItemId)
        {
            var menuItem = await _context.MenuItems.FirstOrDefaultAsync(m => m.Id == menuItemId && !m.IsDeleted);
            if (menuItem == null)
            {
                return false;
            }

            menuItem.IsDeleted = true;
            menuItem.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        // Set availability of a menu item
        public async Task<bool> SetAvailability(int menuItemId, bool isAvailable)
        {
            var menuItem = await _context.MenuItems.FirstOrDefaultAsync(m => m.Id == menuItemId && !m.IsDeleted);
            if (menuItem == null)
            {
                return false;
            }

            menuItem.IsAvailable = isAvailable;
            await _context.SaveChangesAsync();
            return true;
        }

        // Get menu items by category for a restaurant
        public async Task<List<MenuItemModel>> GetmenubyCategory(int restaurantId, string category)
        {
            return await _context.MenuItems
                .Where(m => m.RestaurantId == restaurantId && !m.IsDeleted && m.Category == category)
                .ToListAsync();
        }

        // Update the price of a menu item
        public async Task<bool> UpdatePrice(int menuItemId, decimal newPrice)
        {
            var menuItem = await _context.MenuItems.FirstOrDefaultAsync(m => m.Id == menuItemId && !m.IsDeleted);
            if (menuItem == null)
            {
                return false;
            }

            menuItem.Price = newPrice;
            await _context.SaveChangesAsync();
            return true;
        }

        // Apply a discount percentage to a menu item
        public async Task<bool> ApplyDiscount(int menuItemId, decimal discountPercentage)
        {
            var menuItem = await _context.MenuItems.FirstOrDefaultAsync(m => m.Id == menuItemId && !m.IsDeleted);
            if (menuItem == null)
            {
                return false;
            }

            menuItem.DiscountPercentage = discountPercentage;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
