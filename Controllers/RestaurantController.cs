using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApp.Models;
using RestaurantApp.Services;

[ApiController]
[Route("api/restaurants")]
public class RestaurantController : ControllerBase
{
    private readonly IRestaurantService _restaurantService;

    public RestaurantController(IRestaurantService restaurantService)
    {
        _restaurantService = restaurantService;
    }

    /// <summary>Get all active restaurants.</summary>
    [HttpGet]
    public async Task<IActionResult> GetRestaurants()
    {
        var restaurants = await _restaurantService.GetAllRestaurants();
        return Ok(restaurants);
    }

    /// <summary>Get restaurant by ID.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRestaurantById(int id)
    {
        var restaurant = await _restaurantService.GetRestaurantById(id);
        if (restaurant == null)
            return NotFound(new { message = "Restaurant not found." });
        return Ok(restaurant);
    }

    /// <summary>Get restaurants by city.</summary>
    [HttpGet("city/{city}")]
    public async Task<IActionResult> GetByCity(string city)
    {
        var restaurants = await _restaurantService.GetRestaurantbyCity(city);
        return Ok(restaurants);
    }

    /// <summary>Get restaurants by state.</summary>
    [HttpGet("state/{state}")]
    public async Task<IActionResult> GetByState(string state)
    {
        var restaurants = await _restaurantService.GetRestaurantbyState(state);
        return Ok(restaurants);
    }

    /// <summary>Create a new restaurant. Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateRestaurant([FromBody] RestaurantModel restaurant)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var created = await _restaurantService.CreateRestaurant(restaurant);
        return CreatedAtAction(nameof(GetRestaurantById), new { id = created.Id }, created);
    }

    /// <summary>Update a restaurant. Admin only.</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateRestaurant(int id, [FromBody] RestaurantModel restaurant)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var updated = await _restaurantService.UpdateRestaurant(id, restaurant);
        return Ok(updated);
    }

    /// <summary>Soft-delete a restaurant. Admin only.</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteRestaurant(int id)
    {
        await _restaurantService.DeleteRestaurant(id);
        return Ok(new { message = "Restaurant deleted." });
    }

    /// <summary>Assign a manager to a restaurant. Admin only.</summary>
    [HttpPut("{id}/manager")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignManager(int id, [FromBody] AssignManagerDto dto)
    {
        await _restaurantService.AssignManager(id, dto.ManagerId);
        return Ok(new { message = "Manager assigned." });
    }

    /// <summary>Activate or deactivate a restaurant. Admin only.</summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] SetRestaurantStatusDto dto)
    {
        await _restaurantService.SetRestaurantStatus(id, dto.IsActive);
        return Ok(new { message = $"Restaurant status set to {(dto.IsActive ? "active" : "inactive")}." });
    }
}

public record AssignManagerDto(int ManagerId);
public record SetRestaurantStatusDto(bool IsActive);
