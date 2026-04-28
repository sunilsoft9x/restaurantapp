using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApp.Services;

namespace RestaurantApp.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var claimValue = User.FindFirstValue("id");
            return !string.IsNullOrWhiteSpace(claimValue) && int.TryParse(claimValue, out userId);
        }

        /// <summary>Get the currently authenticated user's profile.</summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Invalid or missing user claim." });

            var user = await _userService.GetUserById(userId);
            if (user == null) return NotFound();
            return Ok(user);
        }

        /// <summary>Get a user by ID. Admin only.</summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null) return NotFound(new { message = $"User {id} not found." });
            return Ok(user);
        }

        /// <summary>Get all users. Admin only.</summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsers();
            return Ok(users);
        }

        /// <summary>Assign a role to a user. Admin only.</summary>
        [HttpPut("{id}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignRole(int id, [FromBody] AssignRoleDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _userService.AssignRole(id, dto.RoleId);
            if (!result) return NotFound(new { message = $"User {id} not found." });
            return Ok(new { message = "Role assigned successfully." });
        }

        /// <summary>Update a user's status (Active/Inactive/Banned). Admin only.</summary>
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _userService.UpdateUserStatus(id, dto.Status);
            if (!result) return NotFound(new { message = $"User {id} not found." });
            return Ok(new { message = "Status updated successfully." });
        }
    }

    public record AssignRoleDto(int RoleId);
    public record UpdateStatusDto(string Status);
}
