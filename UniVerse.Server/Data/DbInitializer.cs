using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace UniVerse.Server.Data
{
    /// <summary>
    /// Database Initializer and Seeder using ADO.NET.
    /// Ensures database schema is created and populated with Marwadi University campus data.
    /// </summary>
    public class DbInitializer
    {
        private readonly AdoNetDbHelper _dbHelper;
        private readonly ILogger<DbInitializer> _logger;

        public DbInitializer(AdoNetDbHelper dbHelper, ILogger<DbInitializer> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing UniVerse SQLite Database via ADO.NET...");

                // 1. Create Schema Tables
                await CreateSchemaAsync();
                await EnsureUserColumnsAsync();
                await EnsureNotificationsTableAsync();

                // 2. Check and Seed Initial Data
                long userCount = await _dbHelper.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM users");
                if (userCount == 0)
                {
                    _logger.LogInformation("Database empty. Seeding Marwadi University campus records...");
                    await SeedCampusDataAsync();
                    _logger.LogInformation("Campus seed data successfully populated.");
                }
                else
                {
                    _logger.LogInformation("Database already contains {Count} users. Skipping seed.", userCount);
                }

                // Ensure Avinash and Archi university student accounts always exist
                var defaultPasswordHash = HashPassword("Password123!");
                await EnsureStudentAccountAsync("avinash.128203@marwadiuniversity.ac.in", "Avinash 128203", "128203", "Hostel D", "304", "+91 98765 43219", defaultPasswordHash);
                await EnsureStudentAccountAsync("archi.kumari126697@marwadiuniversity.ac.in", "Archi Kumari", "126697", "Hostel B", "201", "+91 98765 43220", defaultPasswordHash);

                // 3. Ensure Realistic 7-Day Campus Order Distribution for Live Analytics
                await EnsureRealisticRequestDatesAsync();

                // 4. Ensure Marketplace is 100% Clean for Real Student Postings
                await CleanDummyMarketplaceSeedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize or seed UniVerse database.");
                throw;
            }
        }

        private async Task EnsureUserColumnsAsync()
        {
            try
            {
                var existingCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                await using (var conn = _dbHelper.CreateConnection())
                {
                    await conn.OpenAsync();
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = "PRAGMA table_info(users);";
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        existingCols.Add(reader.GetString(1));
                    }
                }

                var columns = new[]
                {
                    ("avatar_url", "TEXT"),
                    ("department", "TEXT DEFAULT 'Computer Science & Engineering'"),
                    ("semester", "TEXT DEFAULT 'Semester 6'")
                };

                foreach (var (col, def) in columns)
                {
                    if (!existingCols.Contains(col))
                    {
                        await _dbHelper.ExecuteNonQueryAsync($"ALTER TABLE users ADD COLUMN {col} {def};");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Notice: columns check completed");
            }
        }

        private async Task EnsureNotificationsTableAsync()
        {
            try
            {
                await _dbHelper.ExecuteNonQueryAsync(@"
CREATE TABLE IF NOT EXISTS notifications (
    id TEXT PRIMARY KEY,
    user_id TEXT NOT NULL,
    title TEXT NOT NULL,
    message TEXT NOT NULL,
    type TEXT NOT NULL DEFAULT 'system',
    reference_id TEXT,
    is_read INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS idx_notifications_user ON notifications(user_id);
CREATE INDEX IF NOT EXISTS idx_notifications_is_read ON notifications(is_read);
");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Notice: notifications table check completed");
            }
        }

        private async Task EnsureRealisticRequestDatesAsync()
        {
            try
            {
                var updates = new[]
                {
                    ("cccccccc-cccc-cccc-cccc-cccccccccccc", "2026-10-05T07:00:00.0000000Z"),
                    ("4fbbb4ba-8f12-4561-844e-7cff93cd5799", "2026-10-05T11:45:00.0000000Z"),
                    ("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "2026-10-06T08:15:00.0000000Z"),
                    ("fb87f193-7a65-4be8-b71b-c020f6691c96", "2026-10-06T12:50:00.0000000Z"),
                    ("59d4338a-e30e-45e0-89e3-afe531c57c3a", "2026-10-07T05:40:00.0000000Z"),
                    ("e5d3be92-6fe7-486b-8241-2c9f7188c51b", "2026-10-07T11:10:00.0000000Z"),
                    ("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", "2026-10-08T04:45:00.0000000Z"),
                    ("c7d42872-66e4-47f8-ad4c-b093121ac29b", "2026-10-08T09:00:00.0000000Z"),
                    ("c9c0a5d1-aea6-478f-b21d-d4d39d0fee5e", "2026-10-09T07:50:00.0000000Z"),
                    ("200b982c-a2e7-44ea-b644-2dd424939a5b", "2026-10-09T14:15:00.0000000Z"),
                    ("9d2f8f52-4ca2-4714-a02e-815fd6d5047a", "2026-10-10T10:45:00.0000000Z"),
                    ("492c81e1-5638-4a4d-aa13-595597c50cf5", "2026-10-11T05:00:00.0000000Z"),
                    ("af5039a0-635c-4c52-a020-5e7c6c7834da", "2026-10-11T06:15:00.0000000Z")
                };

                foreach (var (id, dt) in updates)
                {
                    await _dbHelper.ExecuteNonQueryAsync(
                        "UPDATE delivery_requests SET created_at = @dt, updated_at = @dt WHERE id = @id;",
                        AdoNetDbHelper.CreateParameter("@dt", dt),
                        AdoNetDbHelper.CreateParameter("@id", id));
                }
                _logger.LogInformation("Campus delivery request timestamps distributed across 7 days successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Notice: EnsureRealisticRequestDatesAsync completed with info.");
            }
        }

        private async Task CleanDummyMarketplaceSeedAsync()
        {
            try
            {
                var seedIds = new[]
                {
                    "d1111111-1111-1111-1111-111111111111",
                    "d2222222-2222-2222-2222-222222222222",
                    "d3333333-3333-3333-3333-333333333333",
                    "d4444444-4444-4444-4444-444444444444",
                    "d5555555-5555-5555-5555-555555555555",
                    "d6666666-6666-6666-6666-666666666666",
                    "d7777777-7777-7777-7777-777777777777",
                    "d8888888-8888-8888-8888-888888888888",
                    "d9999999-9999-9999-9999-999999999999",
                    "daaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    "dbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                    "dccccccc-cccc-cccc-cccc-cccccccccccc",
                    "dddddddd-dddd-dddd-dddd-dddddddddddd",
                    "deeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
                    "dfffffff-ffff-ffff-ffff-ffffffffffff",
                    "e1111111-1111-1111-1111-111111111111",
                    "e2222222-2222-2222-2222-222222222222",
                    "e3333333-3333-3333-3333-333333333333",
                    "e4444444-4444-4444-4444-444444444444"
                };

                foreach (var id in seedIds)
                {
                    await _dbHelper.ExecuteNonQueryAsync("DELETE FROM marketplace_offers WHERE listing_id = @id;", AdoNetDbHelper.CreateParameter("@id", id));
                    await _dbHelper.ExecuteNonQueryAsync("DELETE FROM marketplace_listings WHERE id = @id;", AdoNetDbHelper.CreateParameter("@id", id));
                }

                _logger.LogInformation("Dummy marketplace seed data cleared. Marketplace is clean and ready for student listings.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Notice: CleanDummyMarketplaceSeedAsync completed.");
            }
        }

        private async Task CreateSchemaAsync()
        {
            var schemaPath = Path.Combine(AppContext.BaseDirectory, "Data", "Schema.sql");
            string schemaSql;

            if (File.Exists(schemaPath))
            {
                schemaSql = await File.ReadAllTextAsync(schemaPath);
            }
            else
            {
                // Fallback direct DDL if file path in output directory differs
                schemaSql = @"
CREATE TABLE IF NOT EXISTS users (
    id TEXT PRIMARY KEY,
    email TEXT NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    full_name TEXT NOT NULL,
    enrollment_number TEXT,
    role TEXT NOT NULL DEFAULT 'student',
    hostel_name TEXT,
    room_number TEXT,
    phone_number TEXT,
    department TEXT DEFAULT 'Computer Science & Engineering',
    semester TEXT DEFAULT 'Semester 6',
    avatar_url TEXT,
    is_active_runner INTEGER NOT NULL DEFAULT 0,
    reward_balance REAL NOT NULL DEFAULT 0.0,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS delivery_requests (
    id TEXT PRIMARY KEY,
    requester_id TEXT NOT NULL,
    runner_id TEXT,
    pickup_location TEXT NOT NULL,
    dropoff_location TEXT NOT NULL,
    instructions TEXT,
    total_estimated_amount REAL NOT NULL DEFAULT 0.0,
    delivery_fee REAL NOT NULL DEFAULT 0.0,
    status TEXT NOT NULL DEFAULT 'pending',
    delivery_otp TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    FOREIGN KEY (requester_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (runner_id) REFERENCES users(id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS request_items (
    id TEXT PRIMARY KEY,
    request_id TEXT NOT NULL,
    name TEXT NOT NULL,
    quantity INTEGER NOT NULL DEFAULT 1,
    notes TEXT,
    estimated_price REAL NOT NULL DEFAULT 0.0,
    FOREIGN KEY (request_id) REFERENCES delivery_requests(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS marketplace_listings (
    id TEXT PRIMARY KEY,
    seller_id TEXT NOT NULL,
    title TEXT NOT NULL,
    description TEXT,
    category TEXT NOT NULL,
    condition TEXT NOT NULL,
    price REAL NOT NULL,
    original_price REAL,
    negotiable INTEGER NOT NULL DEFAULT 1,
    pickup_location TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'active',
    image_url TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    FOREIGN KEY (seller_id) REFERENCES users(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS marketplace_offers (
    id TEXT PRIMARY KEY,
    listing_id TEXT NOT NULL,
    buyer_id TEXT NOT NULL,
    offer_price REAL NOT NULL,
    status TEXT NOT NULL DEFAULT 'pending',
    created_at TEXT NOT NULL,
    FOREIGN KEY (listing_id) REFERENCES marketplace_listings(id) ON DELETE CASCADE,
    FOREIGN KEY (buyer_id) REFERENCES users(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_requests_status ON delivery_requests(status);
CREATE INDEX IF NOT EXISTS idx_requests_requester ON delivery_requests(requester_id);
CREATE INDEX IF NOT EXISTS idx_requests_runner ON delivery_requests(runner_id);
CREATE INDEX IF NOT EXISTS idx_marketplace_status ON marketplace_listings(status);
CREATE INDEX IF NOT EXISTS idx_marketplace_seller ON marketplace_listings(seller_id);
CREATE INDEX IF NOT EXISTS idx_offers_listing ON marketplace_offers(listing_id);
";
            }

            await _dbHelper.ExecuteNonQueryAsync(schemaSql);
        }

        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "UniVerseCampusSalt2026"));
            return Convert.ToBase64String(hashedBytes);
        }

        private async Task SeedCampusDataAsync()
        {
            var now = DateTime.UtcNow.ToString("o");
            var defaultPasswordHash = HashPassword("Password123!");
            var adminPasswordHash = HashPassword("AdminPassword123!");

            // IDs for referential integrity
            var student1Id = "11111111-1111-1111-1111-111111111111"; // Aarav Patel
            var runner1Id = "22222222-2222-2222-2222-222222222222";  // Rohit Sharma
            var student2Id = "33333333-3333-3333-3333-333333333333"; // Priya Deshmukh
            var adminId = "44444444-4444-4444-4444-444444444444";    // Campus Admin

            var request1Id = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
            var request2Id = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
            var request3Id = "cccccccc-cccc-cccc-cccc-cccccccccccc";

            await _dbHelper.ExecuteTransactionAsync(async (conn, trans) =>
            {
                // 1. Seed Users (Students, Runners, Admin)
                string insertUserSql = @"
INSERT INTO users (id, email, password_hash, full_name, enrollment_number, role, hostel_name, room_number, phone_number, is_active_runner, reward_balance, created_at, updated_at)
VALUES (@id, @email, @password_hash, @full_name, @enrollment_number, @role, @hostel_name, @room_number, @phone_number, @is_active_runner, @reward_balance, @created_at, @updated_at);";

                var users = new[]
                {
                    new { Id = student1Id, Email = "aarav.patel@marwadiuniversity.ac.in", Pass = defaultPasswordHash, Name = "Aarav Patel", Enr = "92100103001", Role = "student", Hostel = "Hostel D", Room = "304", Phone = "+91 98765 43210", IsRunner = 0, Balance = 250.0 },
                    new { Id = runner1Id, Email = "rohit.sharma@marwadiuniversity.ac.in", Pass = defaultPasswordHash, Name = "Rohit Sharma", Enr = "92100103045", Role = "runner", Hostel = "Hostel D", Room = "212", Phone = "+91 98765 43211", IsRunner = 1, Balance = 580.0 },
                    new { Id = student2Id, Email = "priya.deshmukh@marwadiuniversity.ac.in", Pass = defaultPasswordHash, Name = "Priya Deshmukh", Enr = "92100103088", Role = "student", Hostel = "Hostel B", Room = "108", Phone = "+91 98765 43212", IsRunner = 0, Balance = 120.0 },
                    new { Id = "55555555-5555-5555-5555-555555555555", Email = "avinash.128203@marwadiuniversity.ac.in", Pass = defaultPasswordHash, Name = "Avinash 128203", Enr = "128203", Role = "student", Hostel = "Hostel D", Room = "304", Phone = "+91 98765 43219", IsRunner = 0, Balance = 500.0 },
                    new { Id = "66666666-6666-6666-6666-666666666666", Email = "archi.kumari126697@marwadiuniversity.ac.in", Pass = defaultPasswordHash, Name = "Archi Kumari", Enr = "126697", Role = "student", Hostel = "Hostel B", Room = "201", Phone = "+91 98765 43220", IsRunner = 0, Balance = 500.0 },
                    new { Id = adminId, Email = "admin@marwadiuniversity.ac.in", Pass = adminPasswordHash, Name = "Campus Admin Office", Enr = "MU-ADM-001", Role = "admin", Hostel = "Staff Quarters", Room = "S-12", Phone = "+91 98765 43200", IsRunner = 0, Balance = 0.0 }
                };

                foreach (var u in users)
                {
                    await using var cmd = new SqliteCommand(insertUserSql, conn, trans);
                    cmd.Parameters.AddWithValue("@id", u.Id);
                    cmd.Parameters.AddWithValue("@email", u.Email);
                    cmd.Parameters.AddWithValue("@password_hash", u.Pass);
                    cmd.Parameters.AddWithValue("@full_name", u.Name);
                    cmd.Parameters.AddWithValue("@enrollment_number", u.Enr);
                    cmd.Parameters.AddWithValue("@role", u.Role);
                    cmd.Parameters.AddWithValue("@hostel_name", u.Hostel);
                    cmd.Parameters.AddWithValue("@room_number", u.Room);
                    cmd.Parameters.AddWithValue("@phone_number", u.Phone);
                    cmd.Parameters.AddWithValue("@is_active_runner", u.IsRunner);
                    cmd.Parameters.AddWithValue("@reward_balance", u.Balance);
                    cmd.Parameters.AddWithValue("@created_at", now);
                    cmd.Parameters.AddWithValue("@updated_at", now);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 2. Seed Delivery Requests
                string insertRequestSql = @"
INSERT INTO delivery_requests (id, requester_id, runner_id, pickup_location, dropoff_location, instructions, total_estimated_amount, delivery_fee, status, delivery_otp, created_at, updated_at)
VALUES (@id, @requester_id, @runner_id, @pickup_location, @dropoff_location, @instructions, @total_estimated_amount, @delivery_fee, @status, @delivery_otp, @created_at, @updated_at);";

                var requests = new[]
                {
                    new { Id = request1Id, Requester = student1Id, Runner = (string?)null, Pickup = "Marwadi Central Canteen", Dropoff = "Hostel D, Room 304", Notes = "Leave at door if studying. Extra hot sauce please!", Total = 140.0, Fee = 30.0, Status = "pending", Otp = "4921" },
                    new { Id = request2Id, Requester = student2Id, Runner = (string?)runner1Id, Pickup = "Campus Stationery & Book Depot", Dropoff = "Hostel B, Room 108", Notes = "A2 Engineering Drawing Sheet & 0.5mm Rotring Fineliner", Total = 220.0, Fee = 25.0, Status = "in_transit", Otp = "8134" },
                    new { Id = request3Id, Requester = student1Id, Runner = (string?)runner1Id, Pickup = "Amul Food Court (Ground Floor)", Dropoff = "Hostel D, Room 304", Notes = "Delivered successfully", Total = 95.0, Fee = 20.0, Status = "delivered", Otp = "2048" }
                };

                foreach (var r in requests)
                {
                    await using var cmd = new SqliteCommand(insertRequestSql, conn, trans);
                    cmd.Parameters.AddWithValue("@id", r.Id);
                    cmd.Parameters.AddWithValue("@requester_id", r.Requester);
                    cmd.Parameters.AddWithValue("@runner_id", (object?)r.Runner ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@pickup_location", r.Pickup);
                    cmd.Parameters.AddWithValue("@dropoff_location", r.Dropoff);
                    cmd.Parameters.AddWithValue("@instructions", r.Notes);
                    cmd.Parameters.AddWithValue("@total_estimated_amount", r.Total);
                    cmd.Parameters.AddWithValue("@delivery_fee", r.Fee);
                    cmd.Parameters.AddWithValue("@status", r.Status);
                    cmd.Parameters.AddWithValue("@delivery_otp", r.Otp);
                    cmd.Parameters.AddWithValue("@created_at", now);
                    cmd.Parameters.AddWithValue("@updated_at", now);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 3. Seed Request Items
                string insertItemSql = @"
INSERT INTO request_items (id, request_id, name, quantity, notes, estimated_price)
VALUES (@id, @request_id, @name, @quantity, @notes, @estimated_price);";

                var items = new[]
                {
                    new { Id = Guid.NewGuid().ToString(), ReqId = request1Id, Name = "Crispy Samosa (2 pcs)", Qty = 1, Notes = "Freshly fried", Price = 40.0 },
                    new { Id = Guid.NewGuid().ToString(), ReqId = request1Id, Name = "Cold Bournvita Frappe", Qty = 1, Notes = "With chocolate sprinkles", Price = 100.0 },
                    new { Id = Guid.NewGuid().ToString(), ReqId = request2Id, Name = "A2 Grid Drawing Sheet Pack (x5)", Qty = 1, Notes = "Cartridge 120gsm", Price = 100.0 },
                    new { Id = Guid.NewGuid().ToString(), ReqId = request2Id, Name = "0.5mm Technical Fineliner Pen", Qty = 1, Notes = "Black waterproof", Price = 120.0 },
                    new { Id = Guid.NewGuid().ToString(), ReqId = request3Id, Name = "Amul Cool Cafe Can (250ml)", Qty = 1, Notes = "Chilled", Price = 45.0 },
                    new { Id = Guid.NewGuid().ToString(), ReqId = request3Id, Name = "Amul Masti Spiced Buttermilk (200ml)", Qty = 1, Notes = "Cold", Price = 50.0 }
                };

                foreach (var itm in items)
                {
                    await using var cmd = new SqliteCommand(insertItemSql, conn, trans);
                    cmd.Parameters.AddWithValue("@id", itm.Id);
                    cmd.Parameters.AddWithValue("@request_id", itm.ReqId);
                    cmd.Parameters.AddWithValue("@name", itm.Name);
                    cmd.Parameters.AddWithValue("@quantity", itm.Qty);
                    cmd.Parameters.AddWithValue("@notes", itm.Notes);
                    cmd.Parameters.AddWithValue("@estimated_price", itm.Price);
                    await cmd.ExecuteNonQueryAsync();
                }
            });
        }

        private async Task EnsureStudentAccountAsync(string email, string fullName, string enrollmentNumber, string hostel, string room, string phone, string passwordHash)
        {
            try
            {
                var cleanEmail = email.Trim().ToLowerInvariant();
                var count = await _dbHelper.ExecuteScalarAsync<long>(
                    "SELECT COUNT(*) FROM users WHERE email = @email;",
                    AdoNetDbHelper.CreateParameter("@email", cleanEmail));

                if (count == 0)
                {
                    var id = Guid.NewGuid().ToString();
                    var now = DateTime.UtcNow.ToString("o");
                    await _dbHelper.ExecuteNonQueryAsync(@"
INSERT INTO users (id, email, password_hash, full_name, enrollment_number, role, hostel_name, room_number, phone_number, department, semester, is_active_runner, reward_balance, created_at, updated_at)
VALUES (@id, @email, @password_hash, @full_name, @enrollment_number, 'student', @hostel_name, @room_number, @phone_number, 'Computer Science & Engineering', 'Semester 6', 0, 500.0, @now, @now);",
                        AdoNetDbHelper.CreateParameter("@id", id),
                        AdoNetDbHelper.CreateParameter("@email", cleanEmail),
                        AdoNetDbHelper.CreateParameter("@password_hash", passwordHash),
                        AdoNetDbHelper.CreateParameter("@full_name", fullName),
                        AdoNetDbHelper.CreateParameter("@enrollment_number", enrollmentNumber),
                        AdoNetDbHelper.CreateParameter("@hostel_name", hostel),
                        AdoNetDbHelper.CreateParameter("@room_number", room),
                        AdoNetDbHelper.CreateParameter("@phone_number", phone),
                        AdoNetDbHelper.CreateParameter("@now", now));

                    _logger.LogInformation("Ensured student account {Email} in UniVerse database.", cleanEmail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Notice: EnsureStudentAccountAsync completed for {Email}.", email);
            }
        }
    }
}
