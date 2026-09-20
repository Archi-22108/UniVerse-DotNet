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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize or seed UniVerse database.");
                throw;
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
