# Task Tracker

A full-featured task management web application built with **ASP.NET Core 8 MVC**, **SQL Server**, and raw **ADO.NET** (no ORM).

## Features

| Feature | Details |
|---|---|
| Authentication | Cookie-based login & registration with PBKDF2-SHA256 password hashing |
| Tasks CRUD | Create, view, edit, delete tasks with title, description, status, priority, due date |
| Assignment | Assign tasks to any registered user |
| Comments | Add and delete comments on individual tasks |
| Filtering | Filter task list by status and/or priority |
| Responsive UI | Bootstrap 5 (CDN) + Bootstrap Icons |
| Client validation | jQuery Unobtrusive + native HTML5 validation |

## Tech Stack

- **Framework** – ASP.NET Core 8 MVC
- **Database** – SQL Server (LocalDB for dev, full SQL Server for prod)
- **Data access** – ADO.NET with `Microsoft.Data.SqlClient` (no EF / ORM)
- **Architecture** – Repository pattern with interfaces
- **Auth** – Cookie authentication (no ASP.NET Identity)

---

## Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 8.0+ |
| SQL Server | LocalDB / SQL Server Express / full SQL Server |
| Visual Studio / VS Code / Rider | Any recent version |

---

## Setup Instructions

### 1. Clone / open the project

```bash
git clone <repo-url>
cd TaskTracker
```

### 2. Create the database

Open **SSMS**, **Azure Data Studio**, or the `sqlcmd` CLI and run the migration scripts in order:

```sql
-- Step 1: create schema
:r Database\001_InitialSchema.sql

-- Step 2 (optional): insert sample data
:r Database\002_SeedData.sql
```

Or using `sqlcmd`:

```bash
sqlcmd -S "(localdb)\mssqllocaldb" -i Database\001_InitialSchema.sql
```

### 3. Configure the connection string

The default connection string in `TaskTracker/appsettings.json` targets SQL Server LocalDB:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TaskTrackerDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

To use a different SQL Server instance, update the `Server` value:

```json
"Server=YOUR_SERVER;Database=TaskTrackerDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
```

> **Never commit production credentials.** Use environment variables or `dotnet user-secrets` for sensitive values.

### 4. Run the application

```bash
cd TaskTracker
dotnet run
```

Then open https://localhost:5001 (or the port shown in the console).

---

## Project Structure

```
TaskTracker/
├── Database/
│   ├── 001_InitialSchema.sql      # Tables, indexes, constraints
│   └── 002_SeedData.sql           # Optional sample rows
└── TaskTracker/                   # ASP.NET Core project
    ├── Controllers/
    │   ├── AccountController.cs   # Login, register, logout
    │   ├── CommentsController.cs  # Create / delete comments
    │   ├── HomeController.cs      # Landing page
    │   └── TasksController.cs     # Full task CRUD
    ├── Data/
    │   └── DbConnectionFactory.cs # Wraps SqlConnection creation
    ├── Models/
    │   ├── AppUser.cs
    │   ├── Comment.cs
    │   ├── Enums.cs               # AppTaskStatus, AppTaskPriority
    │   ├── TaskItem.cs
    │   ├── Helpers/
    │   │   └── PasswordHelper.cs  # PBKDF2 hash + verify
    │   └── ViewModels/
    │       ├── LoginViewModel.cs
    │       ├── RegisterViewModel.cs
    │       └── TaskFormViewModel.cs
    ├── Repositories/
    │   ├── Interfaces/            # IUserRepository, ITaskRepository, ICommentRepository
    │   ├── CommentRepository.cs
    │   ├── TaskRepository.cs
    │   └── UserRepository.cs
    ├── Views/
    │   ├── Account/               # Login, Register
    │   ├── Home/                  # Landing page
    │   ├── Shared/                # Layout, validation partial
    │   └── Tasks/                 # Index, Create, Edit, Details, Delete
    └── wwwroot/css/site.css
```

---

## Database Schema

```
Users      (Id, Username, Email, PasswordHash, CreatedAt)
Tasks      (Id, Title, Description, Status, Priority, DueDate,
            CreatedAt, UpdatedAt, CreatedByUserId FK, AssignedToUserId FK)
Comments   (Id, TaskId FK→cascade, UserId FK, Content, CreatedAt)
```

Status values: `0 = To Do`, `1 = In Progress`, `2 = Done`  
Priority values: `0 = Low`, `1 = Medium`, `2 = High`

---

## Development Notes

- Passwords are hashed with PBKDF2 / SHA-256 / 100 000 iterations (built-in `System.Security.Cryptography`).
- All SQL queries use parameterised commands — no string concatenation, no SQL injection risk.
- The `DbConnectionFactory` is registered as a **singleton** (connection string is immutable); repositories are **scoped**.
- Anti-forgery tokens protect every state-changing form.

---

## Production Checklist

- [ ] Replace LocalDB with a production SQL Server instance
- [ ] Store connection string in an environment variable or Azure Key Vault
- [ ] Enable HTTPS redirection and HSTS (`UseHsts` is already wired)
- [ ] Review cookie `SlidingExpiration` and `ExpireTimeSpan` settings
- [ ] Add rate-limiting on the login endpoint
