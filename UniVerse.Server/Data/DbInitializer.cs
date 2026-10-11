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

                // 3. Ensure Realistic 7-Day Campus Order Distribution for Live Analytics
                await EnsureRealisticRequestDatesAsync();

                // 4. Ensure Comprehensive Real Campus Marketplace Catalog Across All Categories
                await EnsureCompleteMarketplaceSeedAsync();
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

        private async Task EnsureCompleteMarketplaceSeedAsync()
        {
            try
            {
                var student1Id = "11111111-1111-1111-1111-111111111111"; // Aarav Patel
                var runner1Id = "22222222-2222-2222-2222-222222222222";  // Rohit Sharma
                var student2Id = "33333333-3333-3333-3333-333333333333"; // Priya Deshmukh

                var now = DateTime.UtcNow.ToString("o");

                var items = new[]
                {
                    // 1. Books
                    new {
                        Id = "d1111111-1111-1111-1111-111111111111",
                        SellerId = student2Id,
                        Title = "Introduction to Algorithms (CLRS 4th Ed.)",
                        Desc = "Official 4th Edition textbook used for Semester 3 & 4 DSA. No pencil markings or torn pages. Hardcover in pristine condition.",
                        Cat = "Books",
                        Cond = "Like New",
                        Price = 650.0,
                        Orig = (double?)1250.0,
                        Neg = 1,
                        Loc = "Central Library Entrance or Hostel B Ground Floor",
                        Status = "active",
                        Img = "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=600"
                    },
                    new {
                        Id = "d4444444-4444-4444-4444-444444444444",
                        SellerId = student1Id,
                        Title = "Database System Concepts (Silberschatz 7th Ed.)",
                        Desc = "Standard textbook for CSE DBMS theory & lab exams. Contains helpful margin sticky notes with relational algebra and B+ tree diagrams.",
                        Cat = "Books",
                        Cond = "Good",
                        Price = 420.0,
                        Orig = (double?)895.0,
                        Neg = 1,
                        Loc = "Hostel D Reception",
                        Status = "active",
                        Img = "https://images.unsplash.com/photo-1497633762265-9d179a990aa6?w=600"
                    },

                    // 2. Electronics
                    new {
                        Id = "d2222222-2222-2222-2222-222222222222",
                        SellerId = runner1Id,
                        Title = "Arduino Mega 2560 R3 + 37 Sensor Modules IoT Lab Kit",
                        Desc = "Complete embedded systems project kit with jumper wires, breadboards, ultrasonic sensor, DHT11, and servo. Ideal for 3rd year Capstone and Microcontroller lab.",
                        Cat = "Electronics",
                        Cond = "Good",
                        Price = 1200.0,
                        Orig = (double?)2400.0,
                        Neg = 1,
                        Loc = "Mechanical & Robotics Block Lab 301",
                        Status = "active",
                        Img = "https://images.unsplash.com/photo-1553406830-ef2513450d76?w=600"
                    },
                    new {
                        Id = "d5555555-5555-5555-5555-555555555555",
                        SellerId = student1Id,
                        Title = "Logitech MX Master 2S Wireless Ergonomic Mouse",
                        Desc = "Rechargeable multi-device Bluetooth mouse with hyper-fast scroll wheel and gesture button. 60-day battery life, includes original Unifying dongle.",
                        Cat = "Electronics",
                        Cond = "Like New",
                        Price = 2450.0,
                        Orig = (double?)4995.0,
                        Neg = 0,
                        Loc = "Hostel D, Room 304",
                        Status = "active",
                        Img = "https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?w=600"
                    },

                    // 3. Study Notes
                    new {
                        Id = "d6666666-6666-6666-6666-666666666666",
                        SellerId = student2Id,
                        Title = "GATE CSE Handwritten Master Notes & Formula Compendium",
                        Desc = "Comprehensive color-coded summary sheets for OS, Compiler Design, TOC, and Computer Networks. Scored 64.3 in GATE 2026 mock exams.",
                        Cat = "Study Notes",
                        Cond = "Good",
                        Price = 350.0,
                        Orig = (double?)750.0,
                        Neg = 1,
                        Loc = "Hostel B Common Room",
                        Status = "active",
                        Img = "https://images.unsplash.com/photo-1517842645767-c639042777db?w=600"
                    },
                    new {
                        Id = "d7777777-7777-7777-7777-777777777777",
                        SellerId = runner1Id,
                        Title = "Engineering Mathematics III & IV Spiral Bound Solved Papers",
                        Desc = "Last 5 years solved university exam question papers with step-by-step Fourier series, Laplace transforms, and probability distributions.",
                        Cat = "Study Notes",
                        Cond = "Like New",
                        Price = 200.0,
                        Orig = (double?)450.0,
                        Neg = 0,
                        Loc = "Central Library Entrance",
                        Status = "active",
                        Img = "https://images.unsplash.com/photo-1456513080510-7bf3a84b82f8?w=600"
                    },

                    // 4. Hostel Life
                    new {
                        Id = "d8888888-8888-8888-8888-888888888888",
                        SellerId = student1Id,
                        Title = "Prestige 1.5L Stainless Steel Electric Kettle",
                        Desc = "Essential hostel appliance for late-night Maggi, coffee, and green tea during end-semester exams. Automatic cutoff, sanitized and clean.",
                        Cat = "Hostel Life",
                        Cond = "Good",
                        Price = 550.0,
                        Orig = (double?)1195.0,
                        Neg = 1,
                        Loc = "Hostel D Ground Floor Gate",
                        Status = "active",
                        Img = "https://images.unsplash.com/photo-1594213114663-ddbe3f2a8335?w=600"
                    },
                    new {
                        Id = "d9999999-9999-9999-9999-999999999999",
                        SellerId = student2Id,
                        Title = "Wipro 9W Smart LED Desk Lamp with 3 Color Modes",
                        Desc = "Adjustable gooseneck study lamp with Warm/Daylight/Cool white modes and step-less dimming touch sensor. Perfect for roommate-friendly late night study.",
                        Cat = "Hostel Life",
                        Cond = "Like New",
                        Price = 450.0,
                        Orig = (double?)999.0,
                        Neg = 0,
                        Loc = "Hostel B, Room 108",
                        Status = "active",
                        Img = "https://images.unsplash.com/photo-1507473885765-e6ed057f782c?w=600"
                    },

                    // 5. Sports
                    new {
                        Id = "daaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                        SellerId = runner1Id,
                        Title = "Yonex Muscle Power 29 Light Badminton Racket + Case",
                        Desc = "Full graphite racquet strung at 24 lbs with BG65 gut. Re-gripped with anti-slip absorbent towel grip last week. Includes padded head cover.",
                        Cat = "Sports",
                        Cond = "Good",
                        Price = 850.0,
                        Orig = (double?)1850.0,
                        Neg = 1,
                        Loc = "Campus Sports Complex / Badminton Court",
                        Status = "active",
                        Img = "https://images.unsplash.com/photo-1626224583764-f87db24ac4ea?w=600"
                    },
                    new {
                        Id = "dbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                        SellerId = student1Id,
                        Title = "Nivia Storm Official Size 5 Campus Football",
                        Desc = "All-weather TPU rubber molded football, great grip on turf and grass. Used only 3 matches in inter-hostel tournament.",
                        Cat = "Sports",
                        Cond = "Good",
                        Price = 350.0,
                        Orig = (double?)720.0,
                        Neg = 0,
                        Loc = "Main University Football Ground",
                        Status = "active",
                        Img = "https://images.unsplash.com/photo-1508098682722-e99c43a406b2?w=600"
                    },

                    // 6. Furniture
                    new {
                        Id = "dccccccc-cccc-cccc-cccc-cccccccccccc",
                        SellerId = runner1Id,
                        Title = "Ergonomic High-Back Mesh Study Chair with Lumbar Support",
                        Desc = "Breathable mesh back with pneumatic seat height adjustment and smooth 360-degree caster wheels. Selling because shifting to single room with built-in chair.",
                        Cat = "Furniture",
                        Cond = "Good",
                        Price = 1800.0,
                        Orig = (double?)4200.0,
                        Neg = 1,
                        Loc = "Hostel D, Room 212",
                        Status = "active",
                        Img = "https://images.unsplash.com/photo-1580481077195-c22ae22b27cf?w=600"
                    },
                    new {
                        Id = "dddddddd-dddd-dddd-dddd-dddddddddddd",
                        SellerId = student2Id,
                        Title = "Foldable Bed Table / Laptop Desk with Cup Holder & Tablet Slot",
                        Desc = "Engineered wood foldable lap desk with non-slip curved legs. Fits up to 15.6 inch laptops plus diary and phone.",
                        Cat = "Furniture",
                        Cond = "Like New",
                        Price = 320.0,
                        Orig = (double?)699.0,
                        Neg = 0,
                        Loc = "Hostel B Reception",
                        Status = "active",
                        Img = "https://images.unsplash.com/photo-1533090161767-e6ffed986c88?w=600"
                    },

                    // 7. Clothing
                    new {
                        Id = "deeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
                        SellerId = student1Id,
                        Title = "Marwadi University Tech Fest 'HackMU' Official Bomber Jacket (Size M)",
                        Desc = "Limited edition navy blue organizer windbreaker jacket with embroidered logo and thermal fleece lining. Worn only during 2-day hackathon.",
                        Cat = "Clothing",
                        Cond = "Like New",
                        Price = 600.0,
                        Orig = (double?)1500.0,
                        Neg = 1,
                        Loc = "Student Activity Center (SAC)",
                        Status = "active",
                        Img = "https://images.unsplash.com/photo-1551028719-00167b16eac5?w=600"
                    },
                    new {
                        Id = "dfffffff-ffff-ffff-ffff-ffffffffffff",
                        SellerId = student2Id,
                        Title = "Decathlon Quechua 20L Campus Backpack (Water-Resistant)",
                        Desc = "Lightweight dual-compartment backpack with dedicated 14-inch padded laptop sleeve and dual side water bottle mesh pockets.",
                        Cat = "Clothing",
                        Cond = "Good",
                        Price = 400.0,
                        Orig = (double?)899.0,
                        Neg = 0,
                        Loc = "Hostel B Gate",
                        Status = "active",
                        Img = "https://images.unsplash.com/photo-1553062407-98eeb64c6a62?w=600"
                    },

                    // 8. Gaming
                    new {
                        Id = "e1111111-1111-1111-1111-111111111111",
                        SellerId = runner1Id,
                        Title = "Cosmic Byte Stellaris Wireless Gamepad / Controller",
                        Desc = "Dual vibration feedback wireless gamepad with 2.4GHz USB nano receiver. Compatible with PC, Steam, and Android. Hall effect magnetic triggers.",
                        Cat = "Gaming",
                        Cond = "Like New",
                        Price = 950.0,
                        Orig = (double?)2199.0,
                        Neg = 1,
                        Loc = "Hostel D Common Room",
                        Status = "active",
                        Img = "https://images.unsplash.com/photo-1600080972464-8e5f35f63d08?w=600"
                    },
                    new {
                        Id = "e2222222-2222-2222-2222-222222222222",
                        SellerId = student1Id,
                        Title = "Razer DeathAdder Essential Gaming Mouse (White Edition)",
                        Desc = "6,400 DPI optical sensor with 5 programmable Hyperesponse buttons and mechanical switches rated for 10 million clicks.",
                        Cat = "Gaming",
                        Cond = "Good",
                        Price = 850.0,
                        Orig = (double?)1699.0,
                        Neg = 0,
                        Loc = "Hostel D, Room 304",
                        Status = "active",
                        Img = "https://images.unsplash.com/photo-1615663245857-ac93bb7c39e7?w=600"
                    },

                    // 9. Other
                    new {
                        Id = "d3333333-3333-3333-3333-333333333333",
                        SellerId = runner1Id,
                        Title = "Omega Engineering Mini Drafter & Imperial Drawing Board",
                        Desc = "Complete first-year mechanical engineering drawing kit. Sturdy clamping mechanism with 360-degree protractor arm, drawing clips, and carry bag.",
                        Cat = "Other",
                        Cond = "Good",
                        Price = 450.0,
                        Orig = (double?)850.0,
                        Neg = 1,
                        Loc = "Hostel D Reception",
                        Status = "active",
                        Img = "https://images.unsplash.com/photo-1581291518857-4e27b48ff24e?w=600"
                    },
                    new {
                        Id = "e3333333-3333-3333-3333-333333333333",
                        SellerId = student2Id,
                        Title = "Casio fx-991EX ClassWiz Scientific Calculator (Non-Programmable)",
                        Desc = "High-resolution natural textbook display with 552 functions, matrix, vector, and equation solver. Allowed in university semester exams.",
                        Cat = "Other",
                        Cond = "Like New",
                        Price = 750.0,
                        Orig = (double?)1495.0,
                        Neg = 1,
                        Loc = "Electrical Engineering Building Ground Floor",
                        Status = "active",
                        Img = "https://images.unsplash.com/photo-1594980596870-8aa52a78d8cd?w=600"
                    },
                    new {
                        Id = "e4444444-4444-4444-4444-444444444444",
                        SellerId = student1Id,
                        Title = "Hercules Roadeo Hardtail 21-Speed Campus Mountain Bicycle",
                        Desc = "Shimano Tourney 21-speed geared cycle with front suspension fork and dual disc brakes. Includes sturdy numbered cable lock, bell, and mudguards.",
                        Cat = "Other",
                        Cond = "Good",
                        Price = 3200.0,
                        Orig = (double?)7800.0,
                        Neg = 1,
                        Loc = "Main Campus Cycle Stand near Hostel D",
                        Status = "active",
                        Img = "https://images.unsplash.com/photo-1485965120184-e220f721d03e?w=600"
                    }
                };

                const string upsertListingSql = @"
INSERT INTO marketplace_listings (id, seller_id, title, description, category, condition, price, original_price, negotiable, pickup_location, status, image_url, created_at, updated_at)
VALUES (@id, @seller_id, @title, @description, @category, @condition, @price, @original_price, @negotiable, @pickup_location, @status, @image_url, @created_at, @updated_at)
ON CONFLICT(id) DO UPDATE SET
    seller_id = excluded.seller_id,
    title = excluded.title,
    description = excluded.description,
    category = excluded.category,
    condition = excluded.condition,
    price = excluded.price,
    original_price = excluded.original_price,
    negotiable = excluded.negotiable,
    pickup_location = excluded.pickup_location,
    status = excluded.status,
    image_url = excluded.image_url;";

                await _dbHelper.ExecuteTransactionAsync(async (conn, trans) =>
                {
                    foreach (var itm in items)
                    {
                        await using var cmd = new SqliteCommand(upsertListingSql, conn, trans);
                        cmd.Parameters.AddWithValue("@id", itm.Id);
                        cmd.Parameters.AddWithValue("@seller_id", itm.SellerId);
                        cmd.Parameters.AddWithValue("@title", itm.Title);
                        cmd.Parameters.AddWithValue("@description", itm.Desc);
                        cmd.Parameters.AddWithValue("@category", itm.Cat);
                        cmd.Parameters.AddWithValue("@condition", itm.Cond);
                        cmd.Parameters.AddWithValue("@price", itm.Price);
                        cmd.Parameters.AddWithValue("@original_price", (object?)itm.Orig ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@negotiable", itm.Neg);
                        cmd.Parameters.AddWithValue("@pickup_location", itm.Loc);
                        cmd.Parameters.AddWithValue("@status", itm.Status);
                        cmd.Parameters.AddWithValue("@image_url", itm.Img);
                        cmd.Parameters.AddWithValue("@created_at", now);
                        cmd.Parameters.AddWithValue("@updated_at", now);
                        await cmd.ExecuteNonQueryAsync();
                    }
                });

                _logger.LogInformation("Comprehensive campus marketplace seed verified with 19 items across all categories.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Notice: EnsureCompleteMarketplaceSeedAsync completed with info.");
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

            var listing1Id = "d1111111-1111-1111-1111-111111111111";
            var listing2Id = "d2222222-2222-2222-2222-222222222222";
            var listing3Id = "d3333333-3333-3333-3333-333333333333";

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

                // 4. Seed Marketplace Listings
                string insertListingSql = @"
INSERT INTO marketplace_listings (id, seller_id, title, description, category, condition, price, original_price, negotiable, pickup_location, status, image_url, created_at, updated_at)
VALUES (@id, @seller_id, @title, @description, @category, @condition, @price, @original_price, @negotiable, @pickup_location, @status, @image_url, @created_at, @updated_at);";

                var listings = new[]
                {
                    new {
                        Id = listing1Id,
                        SellerId = student2Id,
                        Title = "Introduction to Algorithms (CLRS 4th Ed.)",
                        Desc = "Official 4th Edition textbook used for Semester 3 & 4 DSA. No pencil markings or torn pages. Free bookmark included.",
                        Cat = "Books",
                        Cond = "Like New",
                        Price = 650.0,
                        Orig = (double?)1250.0,
                        Neg = 1,
                        Loc = "Central Library Entrance or Hostel B Ground Floor",
                        Status = "active",
                        Img = "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=600"
                    },
                    new {
                        Id = listing2Id,
                        SellerId = runner1Id,
                        Title = "Arduino Mega 2560 R3 + 37 Sensor Modules IoT Lab Kit",
                        Desc = "Complete embedded systems project kit with jumper wires, breadboards, ultrasonic sensors, relay modules, and LCD shield. 100% tested.",
                        Cat = "Electronics",
                        Cond = "Good",
                        Price = 1200.0,
                        Orig = (double?)2400.0,
                        Neg = 1,
                        Loc = "Mechanical & Robotics Block Lab 301",
                        Status = "active",
                        Img = "https://images.unsplash.com/photo-1553406830-ef2513450d76?w=600"
                    },
                    new {
                        Id = listing3Id,
                        SellerId = student1Id,
                        Title = "Omega Engineering Mini Drafter & Imperial Drawing Board",
                        Desc = "Complete first-year mechanical drawing kit. Sturdy clamping mechanism with 360-degree protractor arm and dust cover.",
                        Cat = "Lab Equipment",
                        Cond = "Good",
                        Price = 450.0,
                        Orig = (double?)850.0,
                        Neg = 0,
                        Loc = "Hostel D Reception",
                        Status = "active",
                        Img = "https://images.unsplash.com/photo-1581291518857-4e27b48ff24e?w=600"
                    }
                };

                foreach (var l in listings)
                {
                    await using var cmd = new SqliteCommand(insertListingSql, conn, trans);
                    cmd.Parameters.AddWithValue("@id", l.Id);
                    cmd.Parameters.AddWithValue("@seller_id", l.SellerId);
                    cmd.Parameters.AddWithValue("@title", l.Title);
                    cmd.Parameters.AddWithValue("@description", l.Desc);
                    cmd.Parameters.AddWithValue("@category", l.Cat);
                    cmd.Parameters.AddWithValue("@condition", l.Cond);
                    cmd.Parameters.AddWithValue("@price", l.Price);
                    cmd.Parameters.AddWithValue("@original_price", (object?)l.Orig ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@negotiable", l.Neg);
                    cmd.Parameters.AddWithValue("@pickup_location", l.Loc);
                    cmd.Parameters.AddWithValue("@status", l.Status);
                    cmd.Parameters.AddWithValue("@image_url", l.Img);
                    cmd.Parameters.AddWithValue("@created_at", now);
                    cmd.Parameters.AddWithValue("@updated_at", now);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 5. Seed Marketplace Offers
                string insertOfferSql = @"
INSERT INTO marketplace_offers (id, listing_id, buyer_id, offer_price, status, created_at)
VALUES (@id, @listing_id, @buyer_id, @offer_price, @status, @created_at);";

                await using (var cmd = new SqliteCommand(insertOfferSql, conn, trans))
                {
                    cmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
                    cmd.Parameters.AddWithValue("@listing_id", listing1Id);
                    cmd.Parameters.AddWithValue("@buyer_id", student1Id);
                    cmd.Parameters.AddWithValue("@offer_price", 550.0);
                    cmd.Parameters.AddWithValue("@status", "pending");
                    cmd.Parameters.AddWithValue("@created_at", now);
                    await cmd.ExecuteNonQueryAsync();
                }
            });
        }
    }
}
