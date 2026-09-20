# 📜 UniVerse Development Rules & Strict Architectural Constraints

## 1. 🎯 Mandatory Technology Stack: C# and .NET Only
- **Primary Language & Runtime**: All code written in this repository must strictly use **C#** and the **.NET ecosystem** (`net10.0` / modern .NET).
- **Authorized Frameworks & Libraries**:
  - **C#** (Strictly typed, Microsoft conventions)
  - **ASP.NET Core** (Web API, Minimal APIs, Middleware)
  - **ASP.NET MVC** (Controllers, Models, Views)
  - **ASP.NET Web Forms** (where applicable)
  - **ADO.NET** (`System.Data`, `Microsoft.Data.Sqlite`, `Microsoft.Data.SqlClient`, `DbConnection`, `DbCommand`, `DbParameter`, `DbDataReader`)
- **Prohibited Stacks**: Do not introduce Python, PHP, Ruby, Java, or external backend stacks. All backend business logic and database access must reside exclusively in C# and ADO.NET.

---

## 2. 🔒 Scope Isolation Rule (Single-File / Single-Page Modifications)
- **Targeted Edits Only**: When assigned a task, only modify the specific page, controller, or file that is directly requested or being worked on.
- **Strict Isolation**: 
  - **DO NOT** modify, refactor, or delete code in any other pages or files.
  - **DO NOT** touch existing styling, layouts, or components of unrelated pages.
  - Ensure zero regressions and 100% preservation of all existing functionality across the project.
