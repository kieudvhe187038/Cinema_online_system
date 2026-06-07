


# Technologies Used

## Backend

### ASP.NET Core MVC

ASP.NET Core MVC là framework phát triển ứng dụng web của Microsoft dựa trên mô hình **Model - View - Controller (MVC)**.

Framework được sử dụng để:

* Xây dựng giao diện web động bằng Razor View.
* Xử lý HTTP Request và Response.
* Quản lý luồng xử lý giữa Controller, Service và View.
* Hỗ trợ Dependency Injection (DI).
* Tích hợp Entity Framework Core để làm việc với cơ sở dữ liệu.

---

### Entity Framework Core

Entity Framework Core (EF Core) là ORM (Object Relational Mapping) được sử dụng để tương tác với cơ sở dữ liệu thông qua các đối tượng C#.

---

### SQL Server

SQL Server là hệ quản trị cơ sở dữ liệu quan hệ được sử dụng để lưu trữ dữ liệu của hệ thống.

---

## Frontend

### Razor View Engine

Razor là công nghệ tạo giao diện phía máy chủ (Server-side Rendering) của ASP.NET Core MVC.

---

### Tailwind CSS

Tailwind CSS là framework CSS theo hướng Utility-First, cho phép xây dựng giao diện nhanh chóng bằng cách sử dụng các class có sẵn.

**Ví dụ:**

```html
<button class="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700">
    Save
</button>
```

# Git Workflow & Branching Rules

## 1. Branch Strategy

Dự án sử dụng mô hình phân nhánh đơn giản:

```text
main
 ├── feature/user-management
 ├── feature/movie-management
 ├── feature/showtime-management
 ├── bugfix/login-error
 └── hotfix/security-fix
```

### Main Branch

* `main` là nhánh ổn định.
* Chỉ chứa mã nguồn đã được kiểm tra và hoạt động ổn định.
* Không commit trực tiếp lên `main`.

---

## 2. Naming Convention

### Feature Branch

Cú pháp:

```text
feature/<feature-name>
```

Ví dụ:

```text
feature/user-management
feature/movie-management
```

### Bug Fix Branch

Cú pháp:

```text
bugfix/<bug-name>
```

Ví dụ:

```text
bugfix/login-error
bugfix/date-validation
```

### Hot Fix Branch

Cú pháp:

```text
hotfix/<issue-name>
```

Ví dụ:

```text
hotfix/security-patch
hotfix/database-connection
```

---

## 3. Commit Message Convention

Cấu trúc:

```text
<type>: <description>
```

### Các loại commit

| Type     | Ý nghĩa                         |
| -------- | ------------------------------- |
| feat     | Thêm chức năng mới              |
| fix      | Sửa lỗi                         |
| refactor | Tái cấu trúc mã nguồn           |
| style    | Chỉnh sửa giao diện hoặc format |
| docs     | Cập nhật tài liệu               |
| test     | Thêm hoặc sửa test              |
| chore    | Công việc hỗ trợ, cấu hình      |

### Ví dụ

```text
feat: add doctor management module

feat: create appointment booking page

fix: resolve login validation issue

refactor: simplify appointment service logic

docs: update project structure document

style: improve dashboard layout
```

---

## 4. Development Workflow

### Bước 1: Cập nhật mã nguồn mới nhất

```bash
git checkout main
git pull origin main
```

### Bước 2: Tạo branch mới

```bash
git checkout -b feature/<feature-name>
```

### Bước 3: Thực hiện phát triển

```bash
git add .
git commit -m "feat: add doctor create page"
```

### Bước 4: Push branch

```bash
git push origin feature/<feature-name>
```

### Bước 5: Tạo Pull Request

* Tạo Pull Request vào `main`.
* Chờ review trước khi merge.

---

## 5. Pull Request Rules

Trước khi tạo Pull Request:

* Code phải build thành công.
* Không còn lỗi compile.
* Đã kiểm tra chức năng liên quan.
* Không commit file tạm hoặc file cá nhân.

Ví dụ:

```text
✔ bin/
✔ obj/
✔ .vs/
✔ publish/
```

Không được push lên repository.

---

## 6. Files Ignored By Git

Sử dụng `.gitignore` để loại bỏ:

```text
bin/
obj/
.vs/
publish/
node_modules/

appsettings.Development.json
```

Không commit:

* File build.
* File cache.
* File log.
* File cấu hình cá nhân.

---

## 7. Code Review Rules

Trước khi merge:

* Đọc lại code.
* Kiểm tra naming convention.
* Kiểm tra logic nghiệp vụ.
* Loại bỏ code thừa.
* Không để lại code comment không cần thiết.

Ví dụ không nên:

```csharp
// TODO: Fix later
// Temporary code
```

---

## 8. General Rules

### Nên làm

* Commit nhỏ và rõ ràng.
* Đặt tên branch dễ hiểu.
* Viết commit message có ý nghĩa.
* Pull code mới nhất trước khi làm việc.

### Không nên

* Commit trực tiếp lên `main`.
* Push code chưa chạy được.
* Commit nhiều chức năng trong một commit.
* Đưa thông tin nhạy cảm vào repository.

---

## Recommended Workflow

```text
Pull main
    ↓
Create Feature Branch
    ↓
Develop Feature
    ↓
Commit Changes
    ↓
Push Branch
    ↓
Create Pull Request
    ↓
Code Review
    ↓
Merge Into Main
```


# Design Patterns

### Repository Pattern

Tách biệt logic truy cập dữ liệu khỏi logic nghiệp vụ.

**Lợi ích:**

* Dễ bảo trì.
* Dễ kiểm thử.
* Giảm phụ thuộc vào Entity Framework Core.

---

### Unit of Work Pattern

Quản lý nhiều Repository trong cùng một transaction.

**Lợi ích:**

* Đảm bảo tính nhất quán dữ liệu.
* Giảm số lần truy cập cơ sở dữ liệu.

---

### Dependency Injection (DI)

ASP.NET Core cung cấp cơ chế Dependency Injection tích hợp sẵn.

**Lợi ích:**

* Giảm sự phụ thuộc giữa các thành phần.
* Dễ mở rộng và kiểm thử hệ thống.

---

## Architecture

Dự án được xây dựng theo kiến trúc phân tầng (Layered Architecture):

```text
Presentation Layer
    ↓
Application Layer
    ↓
Domain Layer
    ↓
Infrastructure Layer
    ↓
Database
```

### Presentation Layer

* Controllers
* Views
* ViewModels

### Application Layer

* DTOs
* Services
* Interfaces
* Mappings

### Domain Layer

* Entities
* Business Models

### Infrastructure Layer

* DbContext
* Repositories
* UnitOfWork

Kiến trúc này giúp hệ thống dễ bảo trì, mở rộng và tái sử dụng trong quá trình phát triển.


# Project Structure

```text
Project
├── Controllers
├── Views
│   ├── Home
│   └── Shared
│       ├── _Layout.cshtml
│       └── _ValidationScriptsPartial.cshtml
├── Application
│   ├── Common
│   │   └── Result.cs
│   ├── DTOs
│   │   ├── DoctorDto.cs
│   │   ├── PatientDto.cs
│   │   ├── MedicalServiceDto.cs
│   │   └── AppointmentDto.cs
│   ├── Interfaces
│   │   ├── I...Service.cs
│   │   ├── IGenericRepository.cs
│   │   └── IUnitOfWork.cs
│   ├── Mappings
│   │   └── ...Profile.cs
│   ├── Services
│   │   └── ...Service.cs
│   └── ViewModels
│       └── ...ViewModel.cs
├── Domain
│   └── Entities
├── Infrastructure
│   ├── Data
│   │   └── DbContext.cs
│   ├── Repositories
│   │   ├── GenericRepository.cs
│   │   ├── ...Repository.cs
│   │   └── ...
│   └── UnitOfWork
│       └── UnitOfWork.cs
├── wwwroot
│   ├── css
│   ├── js
│   ├── images
│   └── lib
├── SQL
│   └── CreateAndSeed.sql
├── Program.cs
├── appsettings.json
└── Project.csproj
```

## Layer Responsibilities

### Controllers

* Nhận HTTP Request từ người dùng.
* Gọi Service trong tầng Application.
* Trả về View hoặc Redirect.
* Không truy cập trực tiếp DbContext.
* Không xử lý nghiệp vụ phức tạp.

### Views

* Hiển thị dữ liệu cho người dùng.
* Sử dụng Razor (.cshtml).
* Không chứa business logic.
* Chỉ thực hiện render giao diện.

### Application

Tầng xử lý nghiệp vụ của hệ thống.

#### Common

* Chứa các lớp dùng chung.
* Ví dụ: `Result<T>`, Constants, Helpers.

#### DTOs

* Dùng để trao đổi dữ liệu giữa các tầng.
* Không phụ thuộc vào Entity hoặc View.

#### Interfaces

* Khai báo Service, Repository và UnitOfWork.
* Giúp áp dụng Dependency Injection.

#### Mappings

* Cấu hình AutoMapper.
* Mapping giữa Entity, DTO và ViewModel.

#### Services

* Chứa business logic.
* Điều phối Repository và UnitOfWork.
* Không phụ thuộc vào giao diện MVC.

#### ViewModels

* Dữ liệu phục vụ riêng cho từng View.
* Có thể kết hợp nhiều DTO hoặc dữ liệu giao diện.

### Domain

Chứa các đối tượng cốt lõi của hệ thống.

#### Entities

* Doctor
* Patient
* MedicalService
* Appointment
* ...

Đây là nơi mô tả mô hình nghiệp vụ.

Không phụ thuộc vào:

* MVC
* EF Core
* SQL Server
* AutoMapper

### Infrastructure

Tầng truy cập dữ liệu.

#### Data

* Chứa `DbContext`.
* Cấu hình Entity Framework Core.

#### Repositories

* Thực hiện CRUD với cơ sở dữ liệu.
* Triển khai các Interface Repository.

#### UnitOfWork

* Quản lý transaction.
* Điều phối nhiều Repository trong cùng một phiên làm việc.

### wwwroot

Chứa tài nguyên tĩnh:

* CSS
* JavaScript
* Images
* Thư viện Frontend

### SQL

* Script tạo cơ sở dữ liệu.
* Script seed dữ liệu mẫu.

### Program.cs

* Cấu hình Dependency Injection.
* Middleware.
* Routing.
* Authentication / Authorization.

### appsettings.json

* Connection String.
* Logging.
* Các cấu hình hệ thống.


## Design Principles

* Separation of Concerns (SoC)
* Dependency Injection (DI)
* Repository Pattern
* Unit of Work Pattern
* Layered Architecture
* Clean Architecture Concepts
* Maintainable and Testable Code
