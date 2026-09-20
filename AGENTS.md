# 🏛️ UniVerse Development Guidelines (AGENTS.md)

## 📌 Strict Developer & Agent Rules

### 1. Mandatory .NET & C# Stack
- Every new feature, backend logic, and data layer must be built **exclusively using C# and .NET**.
- Permitted libraries: **C#**, **ASP.NET Core**, **ASP.NET MVC**, **ASP.NET Web Forms**, and **ADO.NET** (`Microsoft.Data.Sqlite`, `System.Data`).
- Do not add or switch to non-.NET languages or runtimes.

### 2. Strict Single-Page & Targeted Modification Constraint
- When working on a specific page, controller, or feature:
  - **Only modify the targeted page/file** currently being developed or fixed.
  - **Do NOT touch, refactor, or edit any other pages or files** in the codebase.
  - All existing UI components, styling, and unrelated logic must remain 100% untouched and preserved.
