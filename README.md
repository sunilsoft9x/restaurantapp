# restaurantapp
A full-stack web application designed to manage restaurant operations including user authentication, menu management, order processing, and role-based access control. Built using modern technologies with a focus on scalability, security, and clean architecture.

# 🍽️ Restaurant Management System

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-4169E1?logo=postgresql&logoColor=white)
![JWT](https://img.shields.io/badge/Auth-JWT-000000?logo=jsonwebtokens&logoColor=white)
![2FA](https://img.shields.io/badge/Security-2FA-FF6F00)
![Swagger](https://img.shields.io/badge/API-Swagger-85EA2D?logo=swagger&logoColor=black)
![Status](https://img.shields.io/badge/Status-Active-2ECC71)
![License](https://img.shields.io/badge/License-MIT-F1C40F)

A **production-ready full-stack Restaurant Management System** built using ASP.NET Core Web API and React.js.  
This application is designed with scalability, clean architecture, and security best practices in mind.

---

# 🚀 Features

## 🔐 Authentication & Authorization
- User Registration & Login
- JWT Token-based Authentication
- Role-based access control (Admin, Staff, Customer)
- Secure password hashing

## 📋 Menu Management
- Add / Update / Delete menu items
- Categorization (Starters, Main Course, etc.)
- Dynamic pricing and availability

## 🛒 Order Management
- Place orders
- Track order status (Pending → Preparing → Completed)
- Fetch user-specific order history

## 🧑‍💼 Admin Capabilities
- Manage users and roles
- Full control over menu and orders
- System extensibility for analytics dashboard

## ⚡ Performance & Scalability
- EF Core optimized queries
- Layered architecture (Controller → Service → Data)
- DTO-based data transfer
- Clean separation of concerns

---

# 🏗️ Tech Stack

## Backend
- ASP.NET Core Web API (.NET 8)
- Entity Framework Core
- PostgreSQL
- JWT Authentication
- Swagger (OpenAPI)

## Frontend
- React.js
- Framer Motion (Animations)
- CSS3 (Modern UI + Dark Mode)

---

# 🧱 Architecture

Client (React)
    ↓
Controllers (API Layer)
    ↓
Services (Business Logic)
    ↓
Repository / DbContext
    ↓
PostgreSQL Database

---

# 📂 Project Structure

backend/
 ├── Controllers/
 ├── Services/
 ├── Models/
 ├── DTOs/
 ├── Data/
 └── Middleware/

frontend/
 ├── components/
 ├── pages/
 ├── services/
 └── styles/

---

# ⚙️ Installation & Setup

## 🔧 Backend

cd backend
dotnet restore
dotnet ef database update
dotnet run

Runs on:
http://localhost:5000

---

## 🎨 Frontend
// In progress and you have to create it youself
cd frontend
npm install
npm run dev


---

# 🔐 Authentication Flow

1. User submits credentials  
2. Server validates credentials  
3. JWT token generated  
4. Token returned to client  
5. Stored in localStorage  
6. Sent in Authorization header  

Example:
Authorization: Bearer <token>

---

# 📡 API Documentation

Swagger UI:
http://localhost:<port>/swagger

---

## 📌 Endpoints

### Auth
POST /api/auth/register  
POST /api/auth/login  

### Users
GET /api/users  
GET /api/users/{id}  
DELETE /api/users/{id}  

### Menu
GET /api/menu  
POST /api/menu  
PUT /api/menu/{id}  
DELETE /api/menu/{id}  

### Orders
POST /api/orders  
GET /api/orders/{id}  
GET /api/orders/user/{userId}  
PUT /api/orders/status/{id}  

---

# 🔒 Security

- JWT Authentication
- Role-based Authorization
- Input validation
- ORM-based SQL Injection protection

---

# 📌 Future Enhancements

- Payment Gateway Integration  
- Real-time updates (SignalR)  
- Mobile App  
- AI-based recommendations  

---

# 🤝 Contribution

Fork → Create Branch → Commit → Push → PR

---

# 📄 License

MIT License

---

# 👨‍💻 Author

Sunil Dhawan 
Professor | Developer

