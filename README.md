# Smart Inventory Management System

A  inventory management web application built with ASP.NET Core MVC, Entity Framework Core, PostgreSQL, ASP.NET Core Identity, Razor Views, and Bootstrap.

The app is branded as **StockPilot** in the UI.

## Features

- User authentication with ASP.NET Core Identity
- Role-based authorization for Admin, Manager, and Staff users
- Product, category, and supplier CRUD workflows
- Product search, category/supplier filters, stock status filters, sorting, and pagination
- Low-stock and out-of-stock alerts
- Dashboard cards for product counts, alerts, inventory value, and recent movements
- Stock movement tracking with business rules that prevent negative inventory
- Audit log records for important actions
- PostgreSQL local development with Docker Compose

## Tech Stack

- ASP.NET Core MVC (.NET 8)
- C#
- Entity Framework Core
- PostgreSQL
- Npgsql EF Core provider
- ASP.NET Core Identity
- Bootstrap
- Razor Views

## Demo Accounts

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@demo.com` | `Demo123!` |
| Manager | `manager@demo.com` | `Demo123!` |
| Staff | `staff@demo.com` | `Demo123!` |

## How to Run Locally

1. Start PostgreSQL on host port `5433`:

   ```powershell
   docker compose up -d
   ```

2. Restore and run the app:

   ```powershell
   C:\Users\Lucas\.dotnet\dotnet.exe restore
   C:\Users\Lucas\.dotnet\dotnet.exe run
   ```

3. Open the URL shown in the terminal.

The app applies EF Core migrations and seeds demo roles, users, categories, suppliers, products, stock movements, and audit logs on startup.

## Database Design

- `Products`: product catalog, SKU, prices, stock levels, category, supplier, active state
- `Categories`: product grouping with delete protection when products exist
- `Suppliers`: vendor/contact records connected to products
- `StockMovements`: inventory history for stock in, stock out, adjustments, sales, returns, and damaged stock
- `Orders` and `OrderItems`: prepared for the next phase of sales order management
- `AuditLogs`: recent actions performed in the system
- `AspNetUsers`, `AspNetRoles`, and related Identity tables: authentication and authorization

## Business Rules

- Product SKU must be unique
- Cost, unit price, stock quantity, and minimum stock cannot be negative
- Products must belong to a category
- Stock cannot be removed below zero
- Every stock adjustment creates a `StockMovement`
- Categories cannot be deleted while products are assigned
- Suppliers with products are marked inactive instead of removed
- Products with stock/order history are marked inactive instead of removed

## Roadmap

- Full order creation flow that reduces stock when completed
- Product image upload
- CSV export
- PDF reports
- Email alerts
- Unit and integration tests
- Deployment documentation
