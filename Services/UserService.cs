using RestaurantApp.Data;
using RestaurantApp.Models;
using RestaurantApp.DTOs;
using Microsoft.EntityFrameworkCore;
namespace RestaurantApp.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IAuthService _authService;
        public UserService(AppDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }
        //constructor done
        //Register User
        public async Task<UserResponseDto> RegisterUser(RegisterDto dto)
        {
            //Check if user already exists
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            //only proceed for new if existing user is null
            if(existingUser != null)
            {
                throw new Exception("User already exists");
            }
            //Hash the password
            var hashedPassword = await _authService.HashPassword(dto.Password);
            //create new user
            //int roleID = 4; //Default to customer
            var user = new UserModel
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = hashedPassword,
                RoleId = 4,
                Status = "Active",
                IsVerified = false,
                IsDeleted = false


            };
            //Save User
            _context.Users.Add(user); // Saves to Dbset - L3
            await _context.SaveChangesAsync(); //Saves to Database - L4

            // Role for Mapping
            await _context.Entry(user).Reference(u => u.Role).LoadAsync();
            //Return User Response DTO
            return MaptoUserResponseDto(user);
        }
        //Validate User
        public async Task<UserModel?> ValidateUser(string email, string password)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null) return null;

            // Check lockout
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
                throw new Exceptions.AccountLockedException(user.LockoutEnd.Value);

            var isPasswordValid = await _authService.VerifyPassword(password, user.PasswordHash);

            if (!isPasswordValid)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                }
                await _context.SaveChangesAsync();
                return null;
            }

            // Successful login — reset counters
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _context.SaveChangesAsync();
            return user;
        }
        //Get User by Id
        public async Task<UserResponseDto?> GetUserById(int id)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return null;
            }
            return MaptoUserResponseDto(user);
        }
        //Get User by Email
        public async Task<UserModel?> GetUserByEmail(string email)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
            return user;
        }
        //Get All Users
        public async Task<List<UserResponseDto>> GetAllUsers()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .ToListAsync();
            return users.Select(u => MaptoUserResponseDto(u)).ToList();
        }

        //Assign Role
        public async Task<bool> AssignRole(int userId, int roleId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return false;
            }
            user.RoleId = roleId;
            await _context.SaveChangesAsync();
            return true;
        }
        //Update User Status
        public async Task<bool> UpdateUserStatus(int userId, string status)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return false;
            }
            user.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        private UserResponseDto MaptoUserResponseDto(UserModel user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                RoleName = user.Role?.Name ?? "Customer",
                Status = user.Status,
                IsVerified = user.IsVerified
            };
        }
    }
}