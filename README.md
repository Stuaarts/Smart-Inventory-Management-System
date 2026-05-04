# StockPilot - Smart Inventory Management System

StockPilot is an inventory management platform for small business teams that need a clear view of products, suppliers, stock levels, sales orders, and inventory activity.

Built with ASP.NET Core MVC, Entity Framework Core, PostgreSQL, ASP.NET Core Identity, Razor Views, and Bootstrap.

## Features

- Secure login and role-based access for Admin, Manager, and Staff users
- Product, category, and supplier management
- Product search, category/supplier filters, stock status filters, sorting, and pagination
- Low-stock and out-of-stock alerts
- Dashboard metrics for inventory value, potential revenue, order activity, and recent stock movements
- Stock movement tracking for receiving, removing, adjusting, selling, returning, and damaged stock
- Sales order creation with draft and completed workflows
- Automatic stock reduction and stock movement records when orders are completed
- Audit log records for important system actions
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

On startup, StockPilot applies EF Core migrations and seeds demo roles, users, categories, suppliers, products, stock movements, and audit logs.

## Render Deployment

The repository includes a `Dockerfile` and `render.yaml` Blueprint for deploying StockPilot on Render.

The Blueprint creates:

- A Docker-based Render web service named `stockpilot`
- A Render Postgres database named `stockpilot-postgres`
- A `DATABASE_URL` environment variable connected to the database's internal connection string

The app reads Render's `PORT` variable, converts Render's PostgreSQL URL into an Npgsql connection string, applies EF Core migrations on startup, and seeds demo data when the database is empty.

Current demo deployment plan:

1. Create a Render Blueprint from this GitHub repository.
2. Select `render.yaml`.
3. Deploy the generated web service and Postgres database.
4. Open the generated `onrender.com` URL and sign in with one of the demo accounts above.

Note: Render's free PostgreSQL database is intended for demos and expires after 30 days.

## Database Design

- `Products`: catalog records with SKU, prices, stock levels, category, supplier, and active status
- `Categories`: product grouping with delete protection when products exist
- `Suppliers`: vendor/contact records connected to products
- `StockMovements`: inventory history for stock in, stock out, adjustments, sales, returns, and damaged stock
- `Orders` and `OrderItems`: sales order records and captured line-item pricing
- `AuditLogs`: tracked system actions
- `AspNetUsers`, `AspNetRoles`, and related Identity tables: authentication and authorization

## Business Rules

- Product SKU must be unique
- Cost, unit price, stock quantity, and minimum stock cannot be negative
- Products must belong to a category
- Stock cannot be reduced below zero
- Every stock adjustment creates a `StockMovement`
- Completing an order reduces product stock and records sale movements
- Draft orders can be cancelled without reducing stock
- Categories cannot be deleted while products are assigned
- Suppliers with products are marked inactive instead of removed
- Products with stock/order history are marked inactive instead of removed

## Future Enhancements

- Production deployment hardening after the demo environment is live
- Product image upload
- CSV export
- PDF reports
- Email alerts
- Unit and integration tests
