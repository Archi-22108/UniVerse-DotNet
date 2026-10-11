# 🏛️ UniVerse — Backend Technical Reference & Professor Viva Guide
**Marwadi University | Department of Computer Science & Engineering**
**Tech Stack:** C# .NET 10 | ASP.NET Core MVC | Pure ADO.NET | Relational SQLite

---

## 📌 1. Sir ko 1-Minute me Samjhane ke liye (Opening Pitch)

> *"Sir, hamara UniVerse project ek **Enterprise 3-Tier Architecture** par based hai jise **C# .NET 10** aur **ASP.NET Core MVC** me banaya gaya hai. Database ke liye university grading guidelines ke mutabiq humne ORM (Entity Framework) ke bajaye pure **ADO.NET** (`SqliteConnection`, `SqliteCommand`, `SqliteDataReader`, `SqliteParameter`, `SqliteTransaction`) use kiya hai. Database relational **SQLite** format me `universe.db` file me store hai. Isme **Repository Pattern**, **Dependency Injection**, **Claims Cookie Authentication**, aur **Anti-Fraud Security Controls** fully implemented hain."*

---

## 📂 2. Kidhar Kya Save Hai? (Directory & File Layout)

| File / Folder Path | Role | Kya Save Hai & Purpose |
| :--- | :--- | :--- |
| `UniVerse.Server/bin/Debug/net10.0/universe.db` | Live Database | **Active SQLite DB File**: Saare students, food orders, items, aur marketplace data yahan real-time save hote hain. |
| `UniVerse.Server/universe.db` | Source DB Copy | Database backup / source template copy. |
| `Data/Schema.sql` | DDL Schema | 6 Tables ke `CREATE TABLE` scripts, Primary Keys, Foreign Keys (Cascades), aur Indexes. |
| `Data/AdoNetDbHelper.cs` | ADO.NET Engine | Core generic ADO.NET execution helper (`SqliteConnection`, `SqliteCommand`, `SqliteDataReader`, `SqliteTransaction`). |
| `Data/Repositories/` | Data Access Layer (DAL) | Database queries run karne ke dedicated repositories: `UserRepository`, `DeliveryRepository`, `MarketplaceRepository`, `NotificationRepository`. |
| `Controllers/` | MVC Controllers | HTTP endpoints: `AccountController` (Auth/OTP), `DeliveryController` (Orders), `DashboardController` (Views). |
| `Models/` & `ViewModels/` | C# Entity Models | C# POCO Classes jo database tables ko represent karti hain (`User.cs`, `DeliveryRequest.cs`, etc.). |
| `Services/EmailVerificationService.cs` | OTP Service | Campus OTP verification engine with instant gateway sync & universal fallback code (`123456`). |
| `Program.cs` | Bootstrap & DI | IoC Container (Singleton/Scoped registrations), Cookie Authentication, Session, aur Swagger API setup. |
| `UniVerse.Server/wwwroot/uploads/avatars/` | Media Storage | Students ki uploaded profile photos / pictures. |
| `UniVerse.Server/bin/Debug/net10.0/logs/sent_emails.log` | Email/OTP Logs | Dispatch kiye gaye verification codes aur logs. |

---

## 🗄️ 3. Database Schema (6 Relational Tables)

1. **`users`**:
   - **Columns:** `id (PK)`, `email (UNIQUE)`, `password_hash`, `full_name`, `enrollment_number`, `role`, `hostel_name`, `room_number`, `reward_balance`, `created_at`, `updated_at`.
   - **Role:** Campus student & runner profiles. Enforces `@marwadiuniversity.ac.in`. Stores PBKDF2 SHA-256 salted password hashes.

2. **`delivery_requests`**:
   - **Columns:** `id (PK)`, `requester_id (FK)`, `runner_id (FK)`, `pickup_location`, `dropoff_location`, `total_estimated_amount`, `delivery_fee`, `status`, `delivery_otp`, `created_at`, `updated_at`.
   - **Role:** Food/package delivery orders. Lifecycle: `pending` ➔ `accepted` ➔ `picked_up` ➔ `completed`.

3. **`request_items`**:
   - **Columns:** `id (PK)`, `request_id (FK)`, `name`, `quantity`, `estimated_price`, `notes`.
   - **Role:** Items ordered within a request (1-to-Many relation with `delivery_requests` with `ON DELETE CASCADE`).

4. **`marketplace_listings`**:
   - **Columns:** `id (PK)`, `seller_id (FK)`, `title`, `description`, `price`, `category`, `status`, `image_url`.
   - **Role:** Campus buy/sell items (books, calculators, gadgets).

5. **`marketplace_offers`**:
   - **Columns:** `id (PK)`, `listing_id (FK)`, `buyer_id (FK)`, `offer_price`, `status`, `created_at`.
   - **Role:** Bargaining/purchase offers made by students.

6. **`notifications`**:
   - **Columns:** `id (PK)`, `user_id (FK)`, `title`, `message`, `type`, `is_read`, `created_at`.
   - **Role:** Real-time updates delivered to student profiles upon order updates or acceptance.

---

## ⚡ 4. Pure ADO.NET Implementation (College Syllabus Requirement)

Hamne **Entity Framework (ORM) ke bajaye pure ADO.NET** use kiya hai:

### 4 Core ADO.NET Objects:
1. `SqliteConnection`: Database connection open aur close karta hai.
2. `SqliteCommand`: Parameterized SQL statement run karta hai.
3. `SqliteParameter`: Input ko strictly sanitize karta hai (100% SQL Injection Proof).
4. `SqliteDataReader`: Rows ko memory-efficient streaming ke through read karta hai.
5. `SqliteTransaction`: Multiple operations ko atomically execute karta hai (ACID compliance).

### C# Code Example (`DeliveryRepository.cs`):
```csharp
public async Task<bool> AcceptRequestAsync(string requestId, string runnerId)
{
    await using var conn = new SqliteConnection(_connectionString);
    await conn.OpenAsync();

    await using var trans = conn.BeginTransaction();
    try
    {
        const string sql = @"UPDATE delivery_requests 
                             SET runner_id = @RunnerId, status = 'accepted', updated_at = @UpdatedAt 
                             WHERE id = @Id AND status = 'pending' AND requester_id != @RunnerId;";

        await using var cmd = new SqliteCommand(sql, conn, trans);
        cmd.Parameters.AddWithValue("@Id", requestId);
        cmd.Parameters.AddWithValue("@RunnerId", runnerId);
        cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow.ToString("o"));

        int affected = await cmd.ExecuteNonQueryAsync();
        if (affected > 0)
        {
            await trans.CommitAsync();
            return true;
        }
        await trans.RollbackAsync();
        return false;
    }
    catch
    {
        await trans.RollbackAsync();
        throw;
    }
}
```

---

## 🛡️ 5. Security & Anti-Fraud Logic

1. **Campus Anti-Fraud Order Guard**:
   - Jo student food order karta hai, woh apna order khud accept nahi kar sakta (`requester_id != currentUserId`).
   - Runner board par uske khud ke orders automatically filter hokar hide ho jaate hain.
2. **Cryptographic Password Hashing**:
   - Passwords database me plain text me save nahi hote.
   - PBKDF2 SHA-256 algorithm (10,000 iterations) with cryptographic 128-bit random salt use hota hai (`DbInitializer.HashPassword`).
3. **Cookie Claims Authentication**:
   - ASP.NET Core built-in Cookie Authentication (`.AspNetCore.Cookies`) with `ClaimsPrincipal`.
   - Claims me `UserId`, `FullName`, `Role`, `HostelName` securely encrypt rehte hain.
4. **Campus Fail-Safe OTP Gateway**:
   - On-screen synchronized OTP badge aur universal viva emergency code (`123456`) configure hai taaki registration viva ke time 100% fail-safe rahe.

---

## 🎓 6. Professor / Viva Questions & Answers (Top 6)

### Q1. Project ka Architecture kya hai?
> **Ans:** *"Sir, hamara project standard **3-Tier MVC Architecture** follow karta hai:
> 1. **Presentation Layer:** Razor Views (`.cshtml`) + Modern CSS UI.
> 2. **Business Logic Layer:** Controllers (`AccountController`, `DeliveryController`) + Services.
> 3. **Data Access Layer (DAL):** Pure ADO.NET repositories jo `universe.db` SQLite relational file se interact karte hain."*

### Q2. Entity Framework kyu nahi use kiya, ADO.NET kyu use kiya?
> **Ans:** *"Sir, university syllabus ka core objective ADO.NET sikhna hai. ADO.NET connection pooling, raw SQL optimization, explicit transaction handling, aur lower memory consumption provide karta hai jo ORM se jyada transparent aur controlled hota hai."*

### Q3. SQL Injection attacks se project kaise secure hai?
> **Ans:** *"Sir, pure project me kahin bhi string concatenation se query execute nahi hoti. Har query me `SqliteParameter` use hota hai (e.g. `cmd.Parameters.AddWithValue(...)`), jisse database engine input ko pure data treat karta hai, executable command nahi."*

### Q4. Dependency Injection (DI) kaise configure kiya hai?
> **Ans:** *"Sir, `Program.cs` me built-in IoC container use kiya hai: `AdoNetDbHelper` ko **Singleton**, aur Repositories ko **Scoped** register kiya hai. Controllers me constructor injection ke through loose coupling ensure hoti hai."*

### Q5. Concurrency kaise handle hoti hai agar do runners ek sath click karein?
> **Ans:** *"Sir, update query me atomic condition check hai: `WHERE id = @Id AND status = 'pending'`. Pehla runner accept karte hi status 'accepted' ho jata hai, jisse second runner ke execute karne par `rowsAffected == 0` return hota hai aur transaction safely rollback ho jata hai."*

### Q6. Live API testing kaise kar sakte hain?
> **Ans:** *"Sir, project me Swagger OpenAPI built-in hai. Browser me `http://localhost:5277/swagger` open karke saare REST API endpoints ko live test kiya ja sakta hai."*

---
*UniVerse Campus Super-App • Marwadi University Viva Defense Document • C# .NET 10 & ADO.NET*
