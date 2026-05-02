# Task Tracker

[![CI](https://github.com/Berkilic41/task-tracker/actions/workflows/ci.yml/badge.svg)](https://github.com/Berkilic41/task-tracker/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)

A full-featured task management web application built with **ASP.NET Core 8 MVC**, **SQL Server**, and raw **ADO.NET** (no ORM).

## Why I Built This

Task management is one of the most universally understood domains — everyone has used a to-do app — which makes it an ideal canvas for demonstrating backend engineering depth. I built Task Tracker to showcase a **complete, layered ASP.NET Core application** with proper authentication, data ownership enforcement, and SQL-level pagination, without relying on an ORM or Identity framework. Every design decision is deliberate: PBKDF2 password hashing, per-user task isolation, brute-force login protection, and a service layer that separates business logic from HTTP concerns.

## 🔑 Technical Highlights

- **PBKDF2-SHA256 password hashing** — 100 000 iterations with random salt + `CryptographicOperations.FixedTimeEquals` (timing-attack resistant); the strongest implementation in the portfolio
- **Service layer** — `TaskService` extracted from controllers; ownership/auth checks centralised; `ITaskService` interface enables unit testing
- **SQL-level pagination** — `COUNT(*) OVER() + OFFSET/FETCH` keeps index page fast at any dataset size
- **Per-user data isolation** — `GetAllAsync` filters by `CreatedByUserId OR AssignedToUserId`; Edit/Delete enforce creator-only access
- **Brute-force protection** — `IMemoryCache` tracks failed login attempts per username; account locked for 15 min after 5 failures
- **Security headers** — `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Content-Security-Policy`, `Referrer-Policy` on every response
- **25+ unit tests** — TaskService (ownership, CRUD, timestamps) with xUnit + Moq
- **Integration tests** — `WebApplicationFactory` verifying security headers, `/health` endpoint, and auth redirects
- **Containerized** — Multi-stage Dockerfile + docker-compose with SQL Server 2022

---

## Features

| Feature | Details |
|---|---|
| Authentication | Cookie-based login & registration with PBKDF2-SHA256 password hashing |
| Brute-force protection | Account lockout after 5 failed attempts (15-min cooldown via IMemoryCache) |
| Tasks CRUD | Create, view, edit, delete tasks with title, description, status, priority, due date |
| Ownership enforcement | Only the creator can edit or delete; creator or assignee can view details |
| Assignment | Assign tasks to any registered user |
| Pagination | SQL-level `OFFSET/FETCH` with page/pageSize query params; default 20 per page |
| Comments | Add and delete comments on individual tasks (creator-only delete) |
| Filtering | Filter task list by status and/or priority |
| Health check | `GET /health` — probes DB connectivity, returns `{ status, database, timestamp }` |
| Responsive UI | Bootstrap 5 (CDN) + Bootstrap Icons |
| Client validation | jQuery Unobtrusive + native HTML5 validation |

---

## Tech Stack

| Layer | Choice |
|---|---|
| Framework | ASP.NET Core 8 MVC |
| Database | SQL Server / LocalDB |
| Data access | ADO.NET — `Microsoft.Data.SqlClient` (no ORM) |
| Architecture | Controller → Service → Repository (3-layer) |
| Auth | Cookie authentication (no ASP.NET Identity) |
| Logging | Serilog — structured logs + request logging |
| Tests | xUnit + Moq (unit) + WebApplicationFactory (integration) |
| Container | Docker + docker-compose |
| CI/CD | GitHub Actions |

---

## Architecture

```
TaskTracker/
├── Controllers/          # Thin HTTP layer — delegates to services
│   ├── AccountController.cs   # Login (rate-limited), register, logout
│   ├── TasksController.cs     # Full task CRUD + pagination
│   └── CommentsController.cs  # Comment create/delete with ownership check
├── Services/             # Business logic + ownership rules
│   ├── Interfaces/ITaskService.cs
│   └── TaskService.cs    # GetOwnedByIdAsync, CreateAsync, UpdateAsync, DeleteAsync
├── Repositories/         # Raw ADO.NET — SQL queries, CancellationToken support
│   ├── Interfaces/
│   ├── TaskRepository.cs
│   ├── UserRepository.cs
│   └── CommentRepository.cs
├── Middleware/
│   └── CorrelationIdMiddleware.cs  # X-Correlation-ID per request
├── Models/
│   ├── TaskItem.cs / AppUser.cs / Comment.cs
│   └── Helpers/PasswordHelper.cs   # PBKDF2 hash + timing-safe verify
└── Data/DbConnectionFactory.cs     # Singleton connection factory
```

---

## Demo Accounts

Run `Database/002_SeedData.sql` to create sample users:

| Username | Password | Role |
|---|---|---|
| `alice` | `Alice123!` | Task creator |
| `bob` | `Bob123!` | Task assignee |

---

## Routes

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/` | Public | Landing page |
| GET/POST | `/Account/Login` | Public | Login (rate-limited: 10/min) |
| GET/POST | `/Account/Register` | Public | Register |
| GET | `/Tasks` | ✅ | Task list (paginated, filterable) |
| GET | `/Tasks/Details/{id}` | ✅ (owner/assignee) | Task detail + comments |
| GET/POST | `/Tasks/Create` | ✅ | Create task |
| GET/POST | `/Tasks/Edit/{id}` | ✅ (creator only) | Edit task |
| POST | `/Tasks/Delete/{id}` | ✅ (creator only) | Delete task |
| POST | `/Comments/Create` | ✅ | Add comment |
| POST | `/Comments/Delete` | ✅ (creator only) | Delete comment |
| GET | `/health` | Public | Health check (DB probe) |

---

## Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 8.0+ |
| SQL Server | LocalDB / SQL Server Express / SQL Server |
| Docker (optional) | For containerised setup |

---

## Getting Started

### Option A — Docker (recommended)

```bash
git clone https://github.com/Berkilic41/task-tracker.git
cd task-tracker
docker-compose up --build
```

Then open http://localhost:8080

### Option B — Local (LocalDB)

```bash
git clone https://github.com/Berkilic41/task-tracker.git
cd task-tracker

# 1. Create the database
sqlcmd -S "(localdb)\mssqllocaldb" -i Database/001_InitialSchema.sql
sqlcmd -S "(localdb)\mssqllocaldb" -i Database/002_SeedData.sql

# 2. Run
cd TaskTracker
dotnet run
```

Connection string is pre-configured for LocalDB in `appsettings.Development.json`.  
For production, override via environment variable: `ConnectionStrings__DefaultConnection`.

---

## Running Tests

```bash
dotnet test
```

Unit tests (TaskService) + integration tests (WebApplicationFactory) run together.

---

## Database Schema

```
Users      (Id, Username, Email, PasswordHash, CreatedAt)
Tasks      (Id, Title, Description, Status, Priority, DueDate,
            CreatedAt, UpdatedAt, CreatedByUserId FK, AssignedToUserId FK)
Comments   (Id, TaskId FK→cascade, UserId FK, Content, CreatedAt)
```

`Status`: `0 = To Do` | `1 = In Progress` | `2 = Done`  
`Priority`: `0 = Low` | `1 = Medium` | `2 = High`

---

## License

MIT — see [LICENSE](LICENSE)
