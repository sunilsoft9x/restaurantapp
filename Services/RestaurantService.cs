using Microsoft.EntityFrameworkCore;
using RestaurantApp.Data;
using RestaurantApp.Exceptions;
using RestaurantApp.Models;

namespace RestaurantApp.Services
{
    public class RestaurantService : IRestaurantService
    {
        private readonly AppDbContext _context;

        public RestaurantService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RestaurantModel> CreateRestaurant(RestaurantModel restaurant)
        {
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();
            return restaurant;
        }

        public async Task<List<RestaurantModel>> GetAllRestaurants()
        {
            return await _context.Restaurants
                .Where(r => !r.IsDeleted)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<RestaurantModel?> GetRestaurantById(int id)
        {
            return await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        }

        public async Task<RestaurantModel> UpdateRestaurant(int restaurantId, RestaurantModel updated)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == restaurantId && !r.IsDeleted)
                ?? throw new NotFoundException("Restaurant", restaurantId);

            restaurant.Name = updated.Name;
            restaurant.Address = updated.Address;
            restaurant.City = updated.City;
            restaurant.State = updated.State;
            restaurant.PinCode = updated.PinCode;
            restaurant.PhoneNumber = updated.PhoneNumber;
            restaurant.Email = updated.Email;
            restaurant.Description = updated.Description;

            await _context.SaveChangesAsync();
            return restaurant;
        }

        public async Task<bool> DeleteRestaurant(int restaurantId)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == restaurantId && !r.IsDeleted)
                ?? throw new NotFoundException("Restaurant", restaurantId);

            restaurant.IsDeleted = true;
            restaurant.IsActive = false;
            restaurant.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignManager(int restaurantId, int userId)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == restaurantId && !r.IsDeleted)
                ?? throw new NotFoundException("Restaurant", restaurantId);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted)
                ?? throw new NotFoundException("User", userId);

            restaurant.ManagerId = userId;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetRestaurantStatus(int restaurantId, bool isActive)
        {
            var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == restaurantId && !r.IsDeleted)
                ?? throw new NotFoundException("Restaurant", restaurantId);

            restaurant.IsActive = isActive;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<RestaurantModel>> GetRestaurantbyCity(string city)
        {
            return await _context.Restaurants
                .Where(r => !r.IsDeleted && r.City.ToLower() == city.ToLower())
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<List<RestaurantModel>> GetRestaurantbyState(string state)
        {
            return await _context.Restaurants
                .Where(r => !r.IsDeleted && r.State.ToLower() == state.ToLower())
                .OrderBy(r => r.Name)
                .ToListAsync();
        }
    }
}
