# CLAUDE.md - Project Rules & Conventions

## 1. Tech Stack & Environment
- **Backend:** .NET 8 / ASP.NET Core MVC (Razor View Engine, Server-side Rendering)
- **Database:** SQL Server via Entity Framework Core (EF Core)
- **Frontend:** Tailwind CSS (Utility-First utility classes only)
- **Architecture:** Layered N-Tier Structure (Presentation -> Application -> Domain -> Infrastructure)

## 2. Naming & Branching Conventions (STRICT)
- **Branch Naming:** `<ten-nguoi>-feature|bugfix|hotfix/<name>` (e.g., `vkieu-feature/user-management`)
- **Commit Format:** `<type>(<ten-nguoi>): <description>` (e.g., `feat(vkieu): add movie management module`)
- **Allowed Types:** `feat`, `fix`, `refactor`, `style`, `docs`, `test`, `chore`
- **Main Rule:** Never commit directly to `main`. Always develop on a feature branch and create PR.

## 3. Architecture & Layer Responsibilities
### Presentation Layer (Controllers, Views, ViewModels)
- **Controllers:** Only handle HTTP requests, call Application Services, and return Views/Redirects. NO DbContext access. NO complex business logic.
- **Views & ViewModels:** Razor views (`.cshtml`) for UI rendering only. ViewModels contain view-specific data.

### Application Layer (Services, Interfaces, DTOs, Mappings)
- **Services:** Contain core business logic, orchestrate Repositories via `IUnitOfWork`. Independent of MVC UI.
- **Interfaces:** Define contracts for Services, `IGenericRepository`, and `IUnitOfWork` to support DI.
- **DTOs & Mappings:** Data transfer objects and AutoMapper profiles for cross-layer data exchange.

### Domain Layer (Entities)
- **Entities:** Core business models (`Doctor`, `Patient`, `MedicalService`, `Appointment`, etc.).
- **Rule:** Absolute POCO. Must NOT depend on MVC, EF Core, AutoMapper, or SQL Server.

### Infrastructure Layer (Data, Repositories, UnitOfWork)
- **Data:** Contains `DbContext` and EF Core configurations.
- **Repositories:** Implement `IGenericRepository` for database CRUD.
- **UnitOfWork:** Manages multi-repository transactions via a single database context session.

## 4. Coding & Review Rules
- **No Todo Comments:** Do NOT leave `// TODO` or temporary scaffolding code in pull requests.
- **Git Hygiene:** Do NOT commit build/cache files (`bin/`, `obj/`, `.vs/`, `publish/`, `node_modules/`, `appsettings.Development.json`).
- **Data Access Rule:** Controllers MUST call Services -> Services MUST call UnitOfWork/Repositories -> Repositories interact with DB. Do NOT bypass layers.

## 5. Automated Knowledge Synchronization (MEM.md Rule)
- Inside the `.claude/` folder, there is a file named `MEM.md` which acts as the shared team memory for all project modifications, bug fixes, database schema changes, and technical decisions.
- **Your Responsibility:** At the end of EVERY task (when you successfully fix a bug, refactor code, change the database, or add a new feature), you MUST automatically inspect, update, or append the modification details into `.claude/MEM.md`.
- **Formatting Rule for MEM.md:** You must preserve the existing content and append a new log entry using the following Markdown structure:
```text
  ### [YYYY-MM-DD] <Feature/Bug Name> (By: <user-name-from-commit>)
  - **What changed:** Short summary of code/DB changes.
  - **Why:** The root cause or technical reason for this change.
  - **Impact/Notes for Team:** Any specific rules, extensions, or side-effects that other team members need to know when working on related modules.