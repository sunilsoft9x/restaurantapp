# RestaurantApp — Future Version Roadmap

---

## v2.0 — Multi-Tenant Foundation & Real-Time Operations

### Goal: Make the platform production-ready for multiple restaurants with live order tracking.

### Auth & Identity
- **Refresh Token rotation** — `RefreshTokenModel` already seeded; implement `POST /api/auth/refresh` and `POST /api/auth/revoke`
- **OTP verify endpoint** — `POST /api/auth/verify-email` using hashed OTP already stored in DB
- **Forgot password flow** — `POST /api/auth/forgot-password` → OTP → `POST /api/auth/reset-password`
- **Google / OAuth2 social login** — via ASP.NET Identity external providers

### Real-Time
- **SignalR hub** — `OrderStatusHub` pushes live order status updates to customer and kitchen dashboard
- **Kitchen display feed** — Operators receive new order notifications without polling

### Restaurant Management
- **Manager self-service** — Managers can update their own restaurant's menu and hours without Admin
- **Operating hours model** — `RestaurantHoursModel` with open/close times per day; orders rejected outside hours
- **Multi-image upload** — Menu item and restaurant cover images stored in Azure Blob / S3

### Infrastructure
- **Redis cache** — Cache `GET /api/restaurants` and menu listings with 5-minute TTL; invalidate on write
- **Structured logging** — Replace `ILogger` console output with Serilog → Seq / Elastic

---

## v3.0 — Customer Experience & Commerce

### Goal: Full consumer-facing feature set — search, reviews, coupons, and payments.

### Discovery
- **Full-text search** — `GET /api/restaurants?q=biryani` using PostgreSQL `tsvector` / ElasticSearch
- **Geo-location filtering** — `GET /api/restaurants?lat=18.9&lng=72.8&radius=5` using PostGIS
- **Cuisine & tag filtering** — Tag restaurants (Italian, Vegan, etc.); filter menu by dietary flags

### Reviews & Ratings
- **Review model** — Customers post star rating + comment after order is `Delivered`
- **Aggregate rating** — Restaurant average rating computed and cached; displayed in list responses
- **Review moderation** — Admin flag/hide abusive reviews

### Coupons & Discounts
- **Coupon service** — `POST /api/coupons` (Admin); validate on order creation; supports % and flat discounts
- **First-order discount** — Auto-apply if customer has no prior completed orders
- **Menu item `discountPercentage`** — Already in schema; wire it into `OrderService` subtotal calculation

### Payments
- **Razorpay / Stripe integration** — `POST /api/payments/initiate` returns payment link; webhook updates `PaymentStatus`
- **Wallet model** — Internal credit wallet for refunds on cancellations
- **Invoice PDF** — Generate downloadable invoice via `QuestPDF` after order delivered

---

## v4.0 — Operations, Analytics & Scaling

### Goal: Give restaurant owners and platform admins data-driven insights and operational control.

### Analytics Dashboard (API layer)
- **Revenue reports** — `GET /api/reports/revenue?restaurantId=1&from=2026-01-01&to=2026-03-31`
- **Top items** — Most ordered menu items per restaurant per period
- **Order volume trends** — Hourly/daily order counts for capacity planning
- **Customer retention** — Repeat vs. new customer ratio per restaurant

### Delivery Management
- **Delivery agent model** — `DeliveryAgentModel` with location; assign agent to order
- **Live GPS tracking** — Agent app pushes coordinates; customer polls `GET /api/orders/{id}/track`
- **ETA calculation** — Estimated delivery time based on distance and kitchen prep time

### Notifications
- **Push notifications** — Firebase FCM integration; notify customer on each status change
- **Email receipts** — Order confirmation and delivery confirmation emails (EmailService already wired)
- **SMS alerts** — Twilio integration for OTP and order status (replace / complement email OTP)

### Scaling
- **Background jobs** — Hangfire for scheduled tasks: auto-cancel unpaid orders after 15 min, daily report emails
- **Database read replicas** — Route `SELECT` queries to read replica via EF Core interceptor
- **API versioning** — `asp-versioning` package; `/api/v1/` and `/api/v2/` routes coexist during migration

---

## v5.0 — Platform & Ecosystem

### Goal: Evolve from a single product into a multi-sided platform with third-party extensibility.

### Multi-App Architecture
- **Microservices split** — Extract `OrderService`, `NotificationService`, `PaymentService` into independent services communicating via MassTransit + RabbitMQ
- **API Gateway** — YARP reverse proxy as single entry point with per-service rate limiting and circuit breaking
- **Event sourcing** — Order lifecycle events published to Kafka topic; audit trail and replay capability

### Marketplace Features
- **Franchises / chains** — One parent `RestaurantChain` entity owning multiple `Restaurant` locations; chain-level analytics
- **Third-party integrations** — Webhook subscriptions (`POST /api/webhooks`) so partners receive order events
- **Public API program** — API keys for external developers; usage metering and billing via Stripe Metered

### Mobile & Web Clients
- **Customer mobile app** — React Native consuming the REST API; deep links for order tracking
- **Restaurant PWA** — Next.js progressive web app for kitchen/manager dashboard; offline order queue
- **Admin portal** — Blazor WebAssembly SPA for platform-level management

### Security & Compliance
- **GDPR / data erasure** — `DELETE /api/users/me` hard-deletes PII; soft-delete already in place for other entities
- **Audit log** — Immutable append-only `AuditLogModel`; every write action recorded with actor, timestamp, diff
- **Penetration testing** — OWASP ZAP automated scan in CI pipeline; block deployment on high-severity findings
- **SOC 2 readiness** — Secrets in Azure Key Vault / AWS Secrets Manager; rotate DB passwords on schedule

---

## Summary Table

| Version | Theme | Key Unlock |
|---------|-------|-----------|
| v2.0 | Multi-Tenant Foundation | Real-time order tracking, refresh tokens, Redis caching |
| v3.0 | Customer Experience | Search, reviews, coupons, payment gateway |
| v4.0 | Operations & Analytics | Delivery agents, revenue reports, push notifications |
| v5.0 | Platform & Ecosystem | Microservices, marketplace, public API program |
