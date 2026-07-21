# Smart Weekly On-Call Rota Management System

A production-ready **ASP.NET Core MVC (.NET 10)** web application for managing weekly on-call rotas, leave requests, shift swaps, and team notifications.

---

## Technology Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core MVC .NET 10 |
| ORM | Entity Framework Core 10 |
| Database | SQL Server / LocalDB |
| Auth | Session-based (BCrypt password hashing) |
| UI | Bootstrap 5.3, Bootstrap Icons 1.11, DataTables 1.13 |
| Password Hashing | BCrypt.Net-Next 4.0 (work factor 12) |

---

## Project Structure

```
OncallRota/
├── Controllers/
│   ├── BaseController.cs          ← shared session helpers (RequireLogin/RequireAdmin)
│   ├── AuthController.cs          ← Login / Logout
│   ├── AdminController.cs         ← Admin dashboard
│   ├── EmployeeController.cs      ← Employee dashboard, MySchedule, Profile
│   ├── EmployeesController.cs     ← CRUD: Employees (admin)
│   ├── RolesController.cs         ← CRUD: Roles
│   ├── TeamsController.cs         ← CRUD: Teams
│   ├── ApplicationsController.cs  ← CRUD: Applications
│   ├── RotationQueueController.cs ← Queue management (add/move-up/move-down/remove)
│   ├── HolidayCalendarController.cs
│   ├── LeaveRequestsController.cs ← Apply, Approve, Reject
│   ├── ShiftSwapController.cs     ← Submit, Approve, Reject + schedule update
│   ├── ScheduleController.cs      ← Generate rota, view, delete entries
│   └── NotificationsController.cs ← View + mark-read (AJAX)
│
├── Models/                        ← 10 EF entities (match DB tables exactly)
├── Data/ApplicationDbContext.cs   ← Fluent API, FK constraints, unique indexes
├── Interfaces/                    ← IRepository<T>, IAuthService, IRotaService, INotificationService
├── Repository/GenericRepository.cs
├── Services/
│   ├── AuthService.cs             ← BCrypt-aware validation + HashPassword helper
│   ├── NotificationService.cs
│   └── RotaService.cs             ← Rota generation with leave-skip logic
├── ViewModels/                    ← 5 typed view models
├── Views/
│   ├── Shared/
│   │   ├── _AdminLayout.cshtml    ← dark sidebar + live notification badge
│   │   └── _EmployeeLayout.cshtml ← employee sidebar + profile link
│   └── ... (35+ Razor views)
└── wwwroot/
    ├── css/site.css
    └── js/site.js
```

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server 2019+ **or** LocalDB (ships with Visual Studio)
- Optional: SQL Server Management Studio (SSMS)

---

## Quick Start

### 1. Create the database and seed data

Open **SSMS** or **sqlcmd** and run the seed script at the repo root:

```sql
-- In SSMS: File → Open → OncallRota_Seed.sql → Execute
```

This creates **OnCallRotaDB** with all tables and sample data including:
- Admin account: `admin@company.com` / `Password123!`
- 6 regular employees across 3 teams
- Rotation queue seeded for Platform Engineering

### 2. Configure connection string *(optional)*

The default connects to LocalDB. Edit `OncallRota/appsettings.json` for a named SQL Server instance:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=OnCallRotaDB;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### 3. Run the application

```powershell
cd OncallRota
dotnet run
```

Navigate to `https://localhost:<port>` — you will land on the login page.

---

## Default Credentials

| Role | Email | Password |
|---|---|---|
| Admin | admin@company.com | Password123! |
| Employee | alice.smith@company.com | Password123! |
| Employee | bob.jones@company.com | Password123! |

> **Note:** Passwords in the seed script are stored as **plain text** for first-run convenience.  
> On the first login the system validates plain text (fallback). Use the Admin → Employees → Edit to save a new password; it will be BCrypt-hashed on save.

---

## Features

### Admin
| Feature | Description |
|---|---|
| Dashboard | 7 live KPI cards + current week schedule |
| Roles | Full CRUD with duplicate prevention |
| Teams | Full CRUD with duplicate prevention |
| Employees | Full CRUD, password auto-hashed on create/edit |
| Applications | Full CRUD, linked to teams |
| Rotation Queue | Add / move-up / move-down / remove per team |
| On-Call Schedule | Generate weekly rota from queue, delete entries |
| Holiday Calendar | Add/edit/soft-delete public holidays |
| Leave Requests | View all, Approve → notification sent, Reject → notification sent |
| Shift Swap | View all, Approve (schedule updated automatically), Reject |
| Notifications | View all employee notifications, mark-read via AJAX |

### Employee
| Feature | Description |
|---|---|
| Dashboard | Current week on-call role, active leave, last 5 notifications |
| My Schedule | Full history of assigned on-call weeks |
| My Profile | Read-only view of own details |
| Leave Requests | Apply for leave, view own history |
| Shift Swap | Submit swap for current week schedule, view history |
| Holiday Calendar | Read-only view of public holidays |
| Notifications | Own notification inbox, mark-read via AJAX |

---

## Architecture Notes

- **BaseController** – abstract controller providing `RequireLogin()` and `RequireAdmin()` session guards, shared `SessionEmployeeId`, `IsAdmin`, etc.
- **BCrypt** – `AuthService.ValidateLoginAsync` supports both `$2...` hashed and legacy plain-text passwords; `AuthService.HashPassword` is called on every new/edited employee password.
- **RotaService** – detects employees on approved leave (overlapping date range) and skips them when assigning Primary/Backup from the queue.
- **Notification badge** – both layouts query the `Notifications` table inline via `Context.RequestServices` to show a live unread badge without a separate API call.
- **DataTables** – all index tables are wired via `.datatable` CSS class; sorting, searching, and pagination are automatic.

---

## Build & Test

```powershell
# Restore and build
dotnet build OncallRota/OncallRota.csproj --configuration Release

# Run (development)
dotnet run --project OncallRota/OncallRota.csproj
```

Expected output: `Build succeeded. 0 Warning(s) 0 Error(s)`

---

## License

Internal use only. Not licensed for public distribution.