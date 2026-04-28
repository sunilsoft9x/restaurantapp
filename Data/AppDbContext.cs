using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using RestaurantApp.Models;
namespace RestaurantApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            /*
            Database configuration typically. In that configuration, you specify:
            Database provider
            Connection string
            Logging options
            Lazy loading
            */
        }
        public DbSet<UserModel> Users { get; set; }
        public DbSet<RoleModel> Roles { get; set; }
        public DbSet<RestaurantModel> Restaurants { get; set; }
        public DbSet<MenuItemModel> MenuItems { get; set; }
        public DbSet<OrderModel> Orders { get; set; }
        public DbSet<OrderItemModel> OrderItems { get; set; }
        public DbSet<OtpVerificationModel> OtpVerifications { get; set; }
        public DbSet<RefreshTokenModel> RefreshTokens { get; set; }


        //Setting up relationships and constraints
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // User-Role relationship - Many to One
            modelBuilder.Entity<UserModel>()
            .HasOne(u => u.Role)//Each user has one role
            .WithMany(r=>r.Users) //Each role can have many users
            .HasForeignKey(u => u.RoleId) //Foreign key in UserModel
            .OnDelete(DeleteBehavior.Restrict); //Don't delete
            //MenuItem-Restaurant relationship - Many to One
            modelBuilder.Entity<MenuItemModel>()
            .HasOne(m=>m.Restaurant) //Each menu item belongs to one restaurant
            .WithMany(r=>r.MenuItems)
            .HasForeignKey(m => m.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);//If restaurant is deleted, delete menu items
            //Order-User relationship - Many to One
            modelBuilder.Entity<OrderModel>()
            .HasOne(o=> o.User)
            .WithMany() //No navigation property in UserModel for orders
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            //Order-Restaurant relationship - Many to One
            modelBuilder.Entity<OrderModel>()
            .HasOne(o => o.Restaurant)
            .WithMany()
            .HasForeignKey(o => o.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);
            //OrderItem-Order relationship - Many to One
            modelBuilder.Entity<OrderItemModel>()
            .HasOne(oi => oi.Order)
            .WithMany(o=>o.OrderItems)
            .HasForeignKey(oi=>oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
            //OrderItem-MenuItem relationship - Many to One
            modelBuilder.Entity<OrderItemModel>()
            .HasOne(oi => oi.MenuItem)
            .WithMany()
            .HasForeignKey(oi=>oi.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);

            // Global soft-delete query filters
            modelBuilder.Entity<UserModel>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<RestaurantModel>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<MenuItemModel>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<OtpVerificationModel>().HasQueryFilter(e => !e.IsDeleted);

            // Decimal precision for monetary fields
            modelBuilder.Entity<MenuItemModel>()
                .Property(m => m.Price).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<MenuItemModel>()
                .Property(m => m.DiscountPercentage).HasColumnType("decimal(5,2)");
            modelBuilder.Entity<OrderModel>()
                .Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<OrderModel>()
                .Property(o => o.GST).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<OrderModel>()
                .Property(o => o.FinalBillAmount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<OrderModel>()
                .Property(o => o.DiscountAmount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<OrderModel>()
                .Property(o => o.DeliveryFee).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<OrderItemModel>()
                .Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)");

            // Database-managed timestamps
            var userCreatedAt = modelBuilder.Entity<UserModel>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()")
                .ValueGeneratedOnAdd();
            userCreatedAt.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

            var userUpdatedAt = modelBuilder.Entity<UserModel>()
                .Property(e => e.UpdatedAt)
                .HasDefaultValueSql("NOW()")
                .ValueGeneratedOnAddOrUpdate();
            userUpdatedAt.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

            var restaurantCreatedAt = modelBuilder.Entity<RestaurantModel>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()")
                .ValueGeneratedOnAdd();
            restaurantCreatedAt.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

            var restaurantUpdatedAt = modelBuilder.Entity<RestaurantModel>()
                .Property(e => e.UpdatedAt)
                .HasDefaultValueSql("NOW()")
                .ValueGeneratedOnAddOrUpdate();
            restaurantUpdatedAt.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

            var menuItemCreatedAt = modelBuilder.Entity<MenuItemModel>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()")
                .ValueGeneratedOnAdd();
            menuItemCreatedAt.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

            var menuItemUpdatedAt = modelBuilder.Entity<MenuItemModel>()
                .Property(e => e.UpdatedAt)
                .HasDefaultValueSql("NOW()")
                .ValueGeneratedOnAddOrUpdate();
            menuItemUpdatedAt.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

            var orderCreatedAt = modelBuilder.Entity<OrderModel>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()")
                .ValueGeneratedOnAdd();
            orderCreatedAt.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

            var orderUpdatedAt = modelBuilder.Entity<OrderModel>()
                .Property(e => e.UpdatedAt)
                .HasDefaultValueSql("NOW()")
                .ValueGeneratedOnAddOrUpdate();
            orderUpdatedAt.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

            var otpCreatedAt = modelBuilder.Entity<OtpVerificationModel>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()")
                .ValueGeneratedOnAdd();
            otpCreatedAt.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

            var refreshTokenCreatedAt = modelBuilder.Entity<RefreshTokenModel>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("NOW()")
                .ValueGeneratedOnAdd();
            refreshTokenCreatedAt.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

            // RefreshToken — User relationship
            modelBuilder.Entity<RefreshTokenModel>()
                .HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RoleModel>().HasData(
                new RoleModel { Id = 1, Name = "Admin" },
                new RoleModel { Id = 2, Name = "Manager" },
                new RoleModel { Id = 3, Name = "Operator" },
                new RoleModel { Id = 4, Name = "Customer" }
            );
        }
        
    }
}