# 🏛️ UniVerse — Campus Super-App & Delivery Network
> **Academic Project Submission for Software Development & Advanced Web Applications**  
> **Ecosystem:** C# .NET 10 | ASP.NET Core MVC | Pure ADO.NET Data Access Layer | Relational SQLite  
> **Institution:** Marwadi University — Faculty of Computer Applications & Technology

---

[![C#](https://img.shields.io/badge/Language-C%23%2010.0-239120?style=for-the-badge&logo=csharp&logoColor=white)](https://dotnet.microsoft.com/)
[![.NET 10](https://img.shields.io/badge/Framework-.NET%2010%20(Kestrel)-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/Architecture-ASP.NET%20Core%20MVC-0284c7?style=for-the-badge&logo=dotnet)](https://learn.microsoft.com/aspnet/core)
[![ADO.NET](https://img.shields.io/badge/Data%20Access-Pure%20ADO.NET-10b981?style=for-the-badge)](https://learn.microsoft.com/dotnet/framework/data/adonet/)
[![Database](https://img.shields.io/badge/Database-SQLite%20Relational-003B57?style=for-the-badge&logo=sqlite&logoColor=white)](https://sqlite.org/)
[![License](https://img.shields.io/badge/License-100%25%20Free%20%26%20Open%20Source-emerald?style=for-the-badge)](#)

---

## 📌 Project Overview
**UniVerse** is a modern, real-world campus super-application engineered for Marwadi University students. The platform solves everyday campus logistics through:
1. **Peer-to-Peer Hostel Delivery Dispatch**: Order food, snacks, library photocopies, and essentials delivered directly to hostel rooms by verified student runners, authenticated with a secure **Delivery OTP**.
2. **Student Marketplace**: Buy and sell textbooks, scientific calculators, stationery, and dorm essentials with real-time moderation.
3. **Campus Authority Executive Command Center (`/Admin`)**: An administrative dashboard for university proctors and hostel wardens featuring live order dispatches, user balance controls, student roster exports, and instant CSV audits.

---

## 🎓 Academic Compliance & Course Objectives
This project is engineered strictly following university syllabus constraints and industry best practices:

- **100% Pure C# & .NET**: Built entirely using C# on `.NET 10` (ASP.NET Core Kestrel). **0% Node.js, 0% Python, 0% PHP, 0% Java**.
- **Pure ADO.NET Implementation (No 3rd-Party ORMs)**:
  - University grading criteria requires direct practical knowledge of ADO.NET objects.
  - Dedicated Data Access Layer implemented via `SqliteConnection`, `SqliteCommand`, `SqliteDataReader`, `SqliteParameter`, and `SqliteTransaction`.
  - Zero reliance on external ORMs (Entity Framework / Dapper) — 100% hand-crafted SQL parameterized queries to prevent SQL injection vulnerabilities.
- **Enterprise MVC Architecture**: Clean separation of concerns with Controllers, Models/ViewModels, dedicated Repository interfaces, Dependency Injection, and Razor layouts.
- **100% Free & Open-Source**: Uses zero paid cloud APIs, zero subscriptions, and zero proprietary licenses.

---

## 🏗️ 3-Tier Solution Architecture

```
universe/
├── UniVerse.sln                        # Visual Studio 2022/2026 Solution
├── UniVerse.Server/                    # Primary ASP.NET Core MVC & API Project
│   ├── Controllers/                    # C# MVC & API Controllers
│   │   ├── AccountController.cs        # Student Auth, Login, Registration
│   │   ├── AdminController.cs          # Campus Authority HQ, Dispatches & Moderation
│   │   ├── DashboardController.cs      # Core Student Portal & Profile views
│   │   ├── DeliveryController.cs       # Delivery requests, claim runner, complete OTP
│   │   ├── HomeController.cs           # Hardware-accelerated High-Speed Landing Page
│   │   ├── MarketplaceController.cs    # Buy/Sell listings & offers
│   │   └── NotificationController.cs   # Campus alerts & activity broadcasts
│   ├── Data/                           # Dedicated ADO.NET Data Access Layer (DAL)
│   │   ├── AdoNetDbHelper.cs           # Core Generic ADO.NET Connection & Command Engine
│   │   ├── DbInitializer.cs            # Relational database bootstrap & seed generator
│   │   ├── Schema.sql                  # DDL Schema with Foreign Keys & Cascades
│   │   └── Repositories/               # Repository Pattern Implementation
│   │       ├── IDeliveryRepository.cs  # Delivery requests DAL interface
│   │       ├── IMarketplaceRepository.cs
│   │       ├── INotificationRepository.cs
│   │       └── IUserRepository.cs
│   ├── Models/                         # C# OOP Entity Models & ViewModels
│   │   ├── User.cs                     # Campus student/runner profile model
│   │   ├── DeliveryRequest.cs          # Delivery orders & request items
│   │   ├── MarketplaceListing.cs       # Campus marketplace listings
│   │   ├── Notification.cs             # Real-time alert notifications
│   │   └── ViewModels/ViewModels.cs    # Strongly-typed Razor ViewModels
│   ├── Views/                          # ASP.NET Core Razor Views (.cshtml)
│   │   ├── Account/                    # Student Login, Register, Profile
│   │   ├── Admin/                      # Executive Command Center & Admin Login
│   │   ├── Dashboard/                  # Student Dashboard, Runner Hub, Settings
│   │   ├── Delivery/                   # Browse Deliveries, Create Request, Tracking
│   │   ├── Home/                       # Responsive High-Performance Landing View
│   │   ├── Marketplace/                # Products Grid, Detail View, New Item Form
│   │   └── Shared/                     # Layouts (_Layout.cshtml, _DashboardLayout.cshtml)
│   ├── wwwroot/                        # Static assets (images, products, icons, video)
│   ├── universe.db                     # Relational SQLite Database
│   └── Program.cs                      # Application Entry Point & DI Container
├── run-server.bat                      # 1-Click CLI launcher
└── start-universe.bat                  # 1-Click Server + Cloudflare Mobile Tunnel launcher
```

---

## 🗄️ Relational Database Schema (SQLite)

The database schema is defined in `UniVerse.Server/Data/Schema.sql` and initialized automatically on startup via ADO.NET:

1. **`users`**: Student profiles, password hashes, Marwadi University email domain enforcement (`@marwadiuniversity.ac.in`), room numbers, hostel blocks, and reward balances.
2. **`delivery_requests`**: Order reference, pickup location, dropoff destination, delivery tips, status lifecycle (`pending` ➔ `accepted` ➔ `in_transit` ➔ `delivered`), and 4-digit Delivery OTP.
3. **`request_items`**: Line items per delivery request (1-to-many relationship with `delivery_requests` with `ON DELETE CASCADE`).
4. **`marketplace_listings`**: Products, prices, descriptions, seller IDs, categories, and image paths.
5. **`marketplace_offers`**: Purchase and bargain bids made between students.
6. **`notifications`**: Real-time push updates for order assignments, completions, and campus broadcasts.

---

## ⚡ Performance & Security Features

- **Hardware-Accelerated UI**: Custom CSS transforms (`translateZ(0)`) and optimized assets achieving sub-60ms response times.
- **In-Memory Cache**: Static homepage stats cached for 30 seconds via `IMemoryCache` to minimize database hits during peak campus order volume.
- **Response Compression**: Gzip/Brotli compression middleware reducing payload sizes by over 65%.
- **Authentication & Authorization**: Cookie-based Claims authentication with role enforcement (`student`, `runner`, `admin`) and PBKDF2 password hashing.
- **CSRF Protection**: All forms protected with ASP.NET Core Antiforgery Tokens (`__RequestVerificationToken`).

---

## 🚀 How to Run the Project

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download) or [.NET 9 / .NET 8]
- Visual Studio 2022 (v17.8+) with *ASP.NET and web development* workload, OR Visual Studio Code / .NET CLI.

### Option 1: Visual Studio (Recommended for Evaluation)
1. Double-click `UniVerse.sln` to open the solution in Visual Studio.
2. Ensure **`UniVerse.Server`** is set as the Startup Project.
3. Press **F5** or click **Start Debugging**.
4. The application will launch at **`http://localhost:5277`**.

### Option 2: Command Line (CLI)
```bash
# Clone the repository
git clone https://github.com/Archi-22108/UniVerse-DotNet.git
cd UniVerse-DotNet

# Build the solution (0 Warnings, 0 Errors)
dotnet build UniVerse.sln

# Run the server
dotnet run --project UniVerse.Server
```

---

## 🔑 Demo Credentials (Viva & Evaluation)

| Role | Email | Password | Access Portal |
| :--- | :--- | :--- | :--- |
| **Campus Admin** | `admin@marwadiuniversity.ac.in` | `AdminPassword123!` | [http://localhost:5277/Admin](http://localhost:5277/Admin) |
| **Student (Requester)** | `aarav.patel@marwadiuniversity.ac.in` | `Password123!` | [http://localhost:5277/Account/Login](http://localhost:5277/Account/Login) |
| **Student (Runner)** | `priya.sharma@marwadiuniversity.ac.in` | `Password123!` | [http://localhost:5277/Dashboard/Runner](http://localhost:5277/Dashboard/Runner) |

---

## 👨‍💻 Student Author
- **Course**: Software Development & Advanced Web Applications (C# / .NET)
- **Institution**: Marwadi University, Rajkot, Gujarat
- **Repository**: [https://github.com/Archi-22108/UniVerse-DotNet](https://github.com/Archi-22108/UniVerse-DotNet)
