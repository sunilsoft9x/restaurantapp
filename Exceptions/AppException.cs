namespace RestaurantApp.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
        public NotFoundException(string entity, int id) : base($"{entity} with ID {id} was not found.") { }
    }

    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }

    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message = "Unauthorized access.") : base(message) { }
    }

    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message = "You do not have permission to perform this action.") : base(message) { }
    }

    public class AppValidationException : Exception
    {
        public AppValidationException(string message) : base(message) { }
    }

    public class AccountLockedException : Exception
    {
        public DateTime? LockoutEnd { get; }
        public AccountLockedException(DateTime? lockoutEnd = null)
            : base("Account is locked due to too many failed login attempts.")
        {
            LockoutEnd = lockoutEnd;
        }
    }
}
