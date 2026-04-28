# Plan: RestaurantApp — Production-Grade Upgrade ($10,000)

## TL;DR
The codebase has a solid data model but is far from production-ready. It has no working API beyond a basic Restaurant CRUD (no Auth, Menu, Order controllers), critical security holes (no JWT middleware, no authorization, HTTPS disabled), business logic bugs (50% GST), and missing service implementations. This plan upgrades it across 6 phases: Security Hardening → Missing Controllers & Services → Business Logic → Model Improvements → Advanced Features → Production Readiness.

---

## Phase 1: Security Hardening & Infrastructure (Parallel blocks)

### 1A — JWT + Authentication Middleware
- Add `Jwt` section to `appsettings.json` (SecretKey, Issuer, Audience, ExpirationMinutes)
- Add `appsettings.Development.json` JWT placeholder (use User Secrets)
- Register `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` in `Program.cs`
- Add `app.UseAuthentication()` and `app.UseAuthorization()` BEFORE `app.MapControllers()`
- Fix `AuthService.GenerateJwtToken`: validate that SecretKey is not null/empty, enforce min 256-bit key

### 1B — HTTPS, CORS, Security Headers (parallel with 1A)
- Uncomment `app.UseHttpsRedirection()` (conditionally skip in dev)
- Add `app.UseHsts()` for production
- Add named CORS policy in `Program.cs` (restrict to known origins in prod, allow localhost in dev)
- Add `app.UseCors("RestaurantAppPolicy")`

### 1C — Rate Limiting (parallel with 1A)
- Use built-in .NET 8+ `AddRateLimiter` / fixed window policy
- Apply per-endpoint policy: auth endpoints (5 req/min), general (100 req/min)
- Add `app.UseRateLimiter()`

### 1D — Global Exception Handling (parallel with 1A)
- Create `Middleware/GlobalExceptionMiddleware.cs` (ProblemDetails-formatted error responses)
- Register before all other middleware
- Distinct handling for: `KeyNotFoundException` → 404, `UnauthorizedAccessException` → 403, generic → 500
- Replace all bare `throw new Exception(...)` in services with typed exceptions (custom exception classes: `NotFoundException`, `ConflictException`, `ValidationException`)

### 1E — Swagger Security + JWT UI (parallel)
- Add `AddSecurityDefinition("Bearer", ...)` and `AddSecurityRequirement(...)` to `AddSwaggerGen`
- Add XML doc support in `.csproj` for controller documentation

### 1F — Password Security Upgrade
- Increase PBKDF2 `iterationCount` from 10,000 to 100,000 in `AuthService.HashPassword` and `AuthService.VerifyPassword`

### 1G — OTP Security
- In `OtpService` (new): hash OTP before storing (`SHA256` or PBKDF2), only compare hash at verify time
- OTP code stored as `HashedOtpCode`, not plaintext in `OtpVerificationModel`

---

## Phase 2: Missing Controllers & Service Implementations

### 2A — OtpService Implementation (new file: `Services/OtpService.cs`)
- Implement `IOtpService`: `GenerateOtpCode`, `SaveOtpCode` (hash before save), `VerifyOtpCode` (hash compare), `SendOtp` (calls `IEmailService`)
- Enforce `MaxAttempts` (3), `ExpiryTime` (10 min), block resend within 60s (via `LastOtpSentAt` on UserModel)
- Register in `Program.cs`

### 2B — RestaurantService Implementation (new file: `Services/RestaurantService.cs`)
- Implement all `IRestaurantService` methods (use patterns from `MenuService` as template)
- `CreateRestaurant`, `GetAllRestaurants`, `GetRestaurantById`, `UpdateRestaurant`, `DeleteRestaurant` (soft delete), `AssignManager`, `SetRestaurantStatus`, `GetRestaurantByCity`, `GetRestaurantByState`
- Register in `Program.cs`

### 2C — AuthController (new: `Controllers/AuthController.cs`)
- `POST /api/auth/register` — calls `UserService.RegisterUser`, then sends OTP via `OtpService`
- `POST /api/auth/login` — calls `UserService.ValidateUser`, checks `IsVerified`, account lockout; returns JWT on success
- `POST /api/auth/verify-email` — verifies OTP code, marks `IsVerified = true`
- `POST /api/auth/resend-otp` — rate-limited resend
- `POST /api/auth/forgot-password` — sends OTP for password reset
- `POST /api/auth/reset-password` — verifies OTP + updates password
- `POST /api/auth/refresh` — returns new JWT from refresh token (*Phase 5*)

### 2D — UserController (new: `Controllers/UserController.cs`)
- `GET /api/users/me` — `[Authorize]` — returns own profile
- `GET /api/users/{id}` — `[Authorize(Roles="Admin")]`
- `GET /api/users` — `[Authorize(Roles="Admin")]`
- `PUT /api/users/{id}/role` — `[Authorize(Roles="Admin")]` — calls `UserService.AssignRole`
- `PUT /api/users/{id}/status` — `[Authorize(Roles="Admin")]`

### 2E — MenuController (new: `Controllers/MenuController.cs`)
- `GET /api/restaurants/{restaurantId}/menu` — public
- `GET /api/restaurants/{restaurantId}/menu/category/{cat}` — public
- `POST /api/restaurants/{restaurantId}/menu` — `[Authorize(Roles="Admin,Manager")]`
- `PUT /api/restaurants/{restaurantId}/menu/{id}` — `[Authorize(Roles="Admin,Manager")]`
- `DELETE /api/restaurants/{restaurantId}/menu/{id}` — `[Authorize(Roles="Admin,Manager")]`
- `PATCH /api/restaurants/{restaurantId}/menu/{id}/availability` — `[Authorize(Roles="Admin,Manager,Operator")]`
- `PATCH /api/restaurants/{restaurantId}/menu/{id}/discount` — `[Authorize(Roles="Admin,Manager")]`
- `PATCH /api/restaurants/{restaurantId}/menu/{id}/price` — `[Authorize(Roles="Admin,Manager")]`

### 2F — OrderController (new: `Controllers/OrderController.cs`)
- `POST /api/orders` — `[Authorize]` — creates order, userId from JWT claims
- `GET /api/orders/my` — `[Authorize]` — get own orders
- `GET /api/orders/{id}` — `[Authorize]` — get own order by ID, admins see all
- `GET /api/orders/restaurant/{restaurantId}` — `[Authorize(Roles="Admin,Manager,Operator")]`
- `PATCH /api/orders/{id}/status` — `[Authorize(Roles="Admin,Manager,Operator")]`
- `PATCH /api/orders/{id}/delivery-status` — `[Authorize(Roles="Admin,Manager,Operator")]`
- `PATCH /api/orders/{id}/payment-status` — `[Authorize(Roles="Admin,Manager")]`
- `POST /api/orders/{id}/cancel` — `[Authorize]` — user can only cancel own Pending orders

### 2G — Refactor RestaurantController
- Remove direct `AppDbContext` injection; inject `IRestaurantService` instead
- Add `[Authorize]` on write endpoints: Create/Update/Delete → `[Authorize(Roles="Admin")]`
- Status/Manager assign → `[Authorize(Roles="Admin")]`
- Read endpoints remain public

---

## Phase 3: Business Logic Fixes & Improvements

### 3A — Fix Critical Bugs
- **GST Bug**: Change `order.GST = subtotal * 0.5m` → `subtotal * 0.05m` (5%, or configurable via `appsettings.json`)
- **NullReference in MapToOrderResponseDto**: Add `.Include(o => o.Restaurant)` to all Order queries in `OrderService`
- **Dead code after return**: Remove `throw new Exception(...)` lines after `return false` in `OrderService`
- **Status constants**: Add `OrderStatus`, `PaymentStatus`, `DeliveryStatus` static class constants to replace magic strings

### 3B — Order Flow Validation
- Valid status transitions: `Pending → Confirmed → Preparing → Ready → OutForDelivery → Delivered`
- Cancellation only allowed on `Pending` or `Confirmed` orders
- Validate item belongs to the correct restaurant on `CreateOrder`
- Check `menuItem.IsAvailable == true` before adding to order

### 3C — Coupon/Discount Validation
- On `CreateOrder`: if `CouponCode` is provided, validate it exists (new `CouponModel` or simple lookup), apply `DiscountAmount`
- Re-calculate `FinalBillAmount = TotalAmount + GST - DiscountAmount + DeliveryFee`

### 3D — Account Lockout in ValidateUser (UserService)
- Check `LockoutEnd` before validating password
- Increment `FailedLoginAttempts` on wrong password
- Lock account after 5 failures (`LockoutEnd = DateTime.UtcNow.AddMinutes(15)`)
- Reset `FailedLoginAttempts` on successful login

### 3E — Pagination for List Endpoints
- Add `PageNumber`, `PageSize` query params to `GetAllRestaurants`, `GetMenuByRestaurant`, `GetOrdersByUser`, `GetAllUsers`
- Return `PaginatedResponse<T>` wrapper DTO with `TotalCount`, `PageNumber`, `PageSize`, `Data`

---

## Phase 4: Data Model & DB Improvements

### 4A — EF Core Global Query Filters
- In `AppDbContext.OnModelCreating`: add `.HasQueryFilter(e => !e.IsDeleted)` for `UserModel`, `RestaurantModel`, `MenuItemModel`, `OtpVerificationModel`
- Remove manual `&& !m.IsDeleted` guards in services (handled by filter)

### 4B — Decimal Precision for Currency
- Configure `HasColumnType("decimal(18,2)")` for `Price`, `TotalAmount`, `GST`, `DiscountAmount`, `FinalBillAmount`, `DeliveryFee`, `UnitPrice`, `DiscountPercentage` in `AppDbContext`
- New migration: `AddDecimalPrecisionAndQueryFilters`

### 4C — OtpVerificationModel: Hash OTP
- Rename `OtpCode` column to `HashedOtpCode` in model + migration

### 4D — RestaurantModel: PinCode as String
- Change `PinCode` from `int` to `string` (MaxLength 6) in model + migration

### 4E — UserModel: Add Phone Number
- Add optional `PhoneNumber` (MaxLength 15) field
- Add to `RegisterDto` and `UserResponseDto`

### 4F — Composite Indexes
- `OrderModel`: add Index on `(UserId, CreatedAt)` for user order history queries
- `OrderModel`: add Index on `(RestaurantId, Status)` for restaurant order dashboard
- New migration

---

## Phase 5: Advanced Features

### 5A — Refresh Token
- New model: `RefreshTokenModel` (Token, UserId, ExpiresAt, IsRevoked, CreatedAt)
- Add `DbSet<RefreshTokenModel>` to `AppDbContext`
- `AuthService`: `GenerateRefreshToken()` — cryptographically secure random token
- `POST /api/auth/refresh` endpoint: validates refresh token, issues new JWT + refresh token
- `POST /api/auth/logout` — revokes refresh token

### 5B — Email Templates
- Create `Services/EmailTemplateService.cs` with HTML templates for:
  - Welcome / registration verification OTP
  - Password reset OTP
  - Order confirmation (with order summary)
  - Order status change notification
- Update `OtpService.SendOtp` to use templates

### 5C — Structured Logging (Serilog)
- Add NuGet: `Serilog.AspNetCore`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`
- Configure in `Program.cs`: log to console + rolling file (`logs/restaurant-{Date}.txt`)
- Add request logging middleware (`UseSerilogRequestLogging`)
- Inject `ILogger<T>` in all services for operation logging

### 5D — Input Validation
- Add NuGet: `FluentValidation.AspNetCore`
- Create validators for: `RegisterDto`, `LoginDto`, `OrderCreateDto`, `OrderItemDto`
  - `RegisterDto`: email format, password min 8 chars + complexity, name min 2 chars
  - `LoginDto`: email format, password required
  - `OrderCreateDto`: at least 1 item, valid address, valid contact number
- Register validators in DI

### 5E — Response Caching & Compression
- `builder.Services.AddResponseCompression(...)` with Gzip
- Cache `GetAllRestaurants` (60s), `GetMenuByRestaurant` (30s) with `[ResponseCache]`

---

## Phase 6: Production Readiness

### 6A — Secrets Management
- Move DB password and JWT secret to .NET User Secrets for development
- Document that production should use env vars or Azure Key Vault
- Remove hardcoded password from `appsettings.json` (use placeholder)

### 6B — Health Checks
- Add `builder.Services.AddHealthChecks().AddNpgsql(connectionString)`
- Map `GET /health` endpoint

### 6C — API Versioning
- Add NuGet: `Asp.Versioning.Mvc`
- Add `[ApiVersion("1.0")]` to all controllers
- Configure version routing prefix `/api/v{version}/`

### 6D — Swagger Enhancements
- Add XML documentation comments on all controller actions
- Enable XML doc generation in `.csproj`
- Group endpoints by controller tag

---

## Relevant Files

| File | Change |
|---|---|
| `Program.cs` | Major: add auth middleware, CORS, rate limit, exception handler, Serilog, health checks |
| `appsettings.json` | Add JWT section, remove plaintext password (placeholder) |
| `Services/AuthService.cs` | Fix PBKDF2 iterations, add null validation |
| `Services/UserService.cs` | Add account lockout logic |
| `Services/OrderService.cs` | Fix GST, fix NullRef, add Include(Restaurant), add status validation |
| `Services/OtpService.cs` | NEW — implement IOtpService with OTP hashing |
| `Services/RestaurantService.cs` | NEW — implement IRestaurantService |
| `Services/EmailTemplateService.cs` | NEW — HTML email templates |
| `Controllers/RestaurantController.cs` | Refactor: inject IRestaurantService, add [Authorize] |
| `Controllers/AuthController.cs` | NEW |
| `Controllers/UserController.cs` | NEW |
| `Controllers/MenuController.cs` | NEW |
| `Controllers/OrderController.cs` | NEW |
| `Models/OtpVerificationModel.cs` | Rename OtpCode → HashedOtpCode |
| `Models/RestaurantModel.cs` | PinCode string |
| `Models/UserModel.cs` | Add PhoneNumber |
| `Models/RefreshTokenModel.cs` | NEW |
| `Models/CouponModel.cs` | NEW (optional) |
| `Data/AppDbContext.cs` | Global query filters, decimal precision, new DbSets |
| `Middleware/GlobalExceptionMiddleware.cs` | NEW |
| `DTOs/*.cs` | Add DataAnnotations, add pagination DTOs |
| `RestaurantApp.csproj` | Add Serilog, FluentValidation, Versioning packages |
| New Migration | Decimal precision, OTP hash rename, PinCode string, RefreshToken table, UserPhoneNumber |

---

## Verification

1. `dotnet build` — zero errors
2. `dotnet ef migrations add` — new migration applies cleanly
3. `dotnet ef database update` — DB schema matches
4. Swagger UI at `/swagger`: JWT button visible, all endpoints documented with auth requirements
5. POST `/api/auth/register` → returns 201, OTP email sent
6. POST `/api/auth/login` (unverified) → returns 403
7. POST `/api/auth/verify-email` → returns 200, IsVerified = true
8. POST `/api/auth/login` (verified) → returns JWT
9. GET `/api/restaurants` (no token) → 200 (public)
10. POST `/api/restaurants` (no token) → 401
11. POST `/api/orders` (with JWT) → 201, GST = 5% of subtotal
12. GET `/api/orders/my` (with JWT) → 200, no NullReferenceException
13. POST `/api/auth/login` (wrong password x5) → account locked, 5th attempt returns 423
14. `GET /health` → 200 healthy
15. Rate limit: 6 rapid login attempts → 429 Too Many Requests
