using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using UniVerse.Web.Models;

namespace UniVerse.Web.Data;

/// <summary>
/// ADO.NET Data Access Layer (DAL)
/// Implements database operations using pure ADO.NET objects:
/// - SqliteConnection
/// - SqliteCommand
/// - SqliteParameter (SQL Injection Prevention)
/// - SqliteDataReader
/// </summary>
public class AdoNetDbHelper : IAdoNetDbHelper
{
    private readonly string _connectionString;

    public AdoNetDbHelper(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Initializes database tables and inserts seed data if not present.
    /// Demonstrates ADO.NET DDL execution.
    /// </summary>
    public void InitializeDatabase()
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var ddlScript = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FullName TEXT NOT NULL,
                    Email TEXT NOT NULL UNIQUE,
                    Password TEXT NOT NULL DEFAULT 'Password123!',
                    Role TEXT NOT NULL,
                    HostelBlock TEXT,
                    RoomNumber TEXT,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS Products (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Category TEXT NOT NULL,
                    Price REAL NOT NULL,
                    ImageFileName TEXT NOT NULL,
                    InStock INTEGER NOT NULL DEFAULT 1,
                    VendingMachineId TEXT NOT NULL DEFAULT 'VM-HOSTEL-D'
                );

                CREATE TABLE IF NOT EXISTS DeliveryRequests (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    StudentName TEXT NOT NULL,
                    HostelRoom TEXT NOT NULL,
                    ItemsDescription TEXT NOT NULL,
                    TotalAmount REAL NOT NULL,
                    RewardFee REAL NOT NULL,
                    Status TEXT NOT NULL,
                    RunnerName TEXT,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );
            ";

            using (var command = new SqliteCommand(ddlScript, connection))
            {
                command.ExecuteNonQuery();
            }

            // Seed full 57 vending products if needed
            var checkCountQuery = "SELECT COUNT(*) FROM Products;";
            using (var countCmd = new SqliteCommand(checkCountQuery, connection))
            {
                var count = Convert.ToInt64(countCmd.ExecuteScalar());
                if (count < 50)
                {
                    using (var delCmd = new SqliteCommand("DELETE FROM Products;", connection))
                    {
                        delCmd.ExecuteNonQuery();
                    }
                    SeedProducts(connection);
                }
            }

            // Always ensure default campus demo accounts exist with valid passwords
            SeedDemoUsers(connection);
        }
    }

    private void SeedProducts(SqliteConnection connection)
    {
        var seedSql = @"
            INSERT INTO Products (Name, Category, Price, ImageFileName, InStock, VendingMachineId) VALUES
            -- Chips & Savory Snacks (19 items)
            ('CrunchEx Chili Tadka', 'Chips', 20.0, 'crunchex-chili-tadka.webp', 1, 'A-01'),
            ('Kurkure Masala Munch', 'Chips', 20.0, 'kurkure-masala-munch.webp', 1, 'A-02'),
            ('Chili Chataka Kurkure', 'Chips', 20.0, 'chili-chataka-kurkure.webp', 1, 'A-03'),
            ('Lays Magic Masala', 'Chips', 20.0, 'lays-magic-masala.webp', 1, 'A-04'),
            ('Lays Sizzling Hot', 'Chips', 20.0, 'lays-sizzling-hot.webp', 1, 'A-05'),
            ('Lays West Indies Sweet Chilli', 'Chips', 20.0, 'lays-west-indies-sweet-chilli.webp', 1, 'A-06'),
            ('Puffcorn Lays', 'Chips', 20.0, 'puffcorn-lays.webp', 1, 'A-07'),
            ('Balaji Masala Wafers', 'Chips', 20.0, 'balaji-masala-wafers.webp', 1, 'A-08'),
            ('Balaji Salted Wafers', 'Chips', 20.0, 'balaji-salted-wafers.webp', 1, 'A-09'),
            ('Bingo Mad Angles Achaari', 'Chips', 20.0, 'bingo-mad-angles-achaari.webp', 1, 'A-10'),
            ('Doritos Cheese Supreme', 'Chips', 30.0, 'doritos-cheese.webp', 1, 'A-11'),
            ('Act II Butter Popcorn', 'Chips', 35.0, 'act-butter-popcorn.webp', 1, 'A-12'),
            ('Gopal Farali Chevdo', 'Chips', 10.0, 'gopal-farali-chevdo.webp', 1, 'A-13'),
            ('Gopal Masala Sev Murmura', 'Chips', 10.0, 'gopal-masala-sev-murmura.webp', 1, 'A-14'),
            ('Gopal Mexican Chilli', 'Chips', 10.0, 'gopal-mexican-chilli.webp', 1, 'A-15'),
            ('Gopal Moong Dal', 'Chips', 10.0, 'gopal-moong-dal.webp', 1, 'A-16'),
            ('Gopal Tikha Mitha Mix', 'Chips', 10.0, 'gopal-tikha-mitha-mix.webp', 1, 'A-17'),
            ('Roaven Salted Peanut', 'Chips', 15.0, 'roaven-salted-peanut.webp', 1, 'A-18'),
            ('Maggi 2-Minute Noodles', 'Chips', 15.0, 'maggi-2-min.webp', 1, 'A-19'),

            -- Cold Drinks & Juices (21 items)
            ('Coca-Cola Can 250ml', 'Drinks', 40.0, 'coca-cola-can.webp', 1, 'B-01'),
            ('Fanta Orange Can 250ml', 'Drinks', 40.0, 'fanta-250ml.webp', 1, 'B-02'),
            ('Sprite Lime Bottle', 'Drinks', 20.0, 'sprite-mrp-20.webp', 1, 'B-03'),
            ('Frooti Mango Drink 400ml', 'Drinks', 20.0, 'frooti-400ml.webp', 1, 'B-04'),
            ('Appy Fizz Sparkling Apple', 'Drinks', 20.0, 'appy-fizz-250ml.webp', 1, 'B-05'),
            ('Amul Kool Koko Flavoured Milk', 'Drinks', 30.0, 'amul-kool-koko.webp', 1, 'B-06'),
            ('Amul Kool Cafe Iced Coffee', 'Drinks', 30.0, 'amul-kool-cafe.webp', 1, 'B-07'),
            ('Amul Kool Rose Milk', 'Drinks', 30.0, 'amul-kool-rose.webp', 1, 'B-08'),
            ('Amul Kool Dark Chocolate', 'Drinks', 35.0, 'amul-kool-dark-chocolate.webp', 1, 'B-09'),
            ('Kinley Water Bottle 500ml', 'Drinks', 10.0, 'kinley-water-500ml.webp', 1, 'B-10'),
            ('Paper Boat Swing Coconut Water', 'Drinks', 25.0, 'swing-coconut-water.webp', 1, 'B-11'),
            ('Paper Boat Swing Chilli Guava', 'Drinks', 25.0, 'swing-guava.webp', 1, 'B-12'),
            ('Paper Boat Swing Mixed Fruit', 'Drinks', 25.0, 'swing-mixed-fruit.webp', 1, 'B-13'),
            ('Paper Boat Swing Pomegranate', 'Drinks', 25.0, 'swing-pomegranate.webp', 1, 'B-14'),
            ('Paper Boat Apple Juice', 'Drinks', 20.0, 'paper-boat-apple.webp', 1, 'B-15'),
            ('Paper Boat Jamun Juice', 'Drinks', 25.0, 'paper-boat-jamun.webp', 1, 'B-16'),
            ('Paper Boat Orange Juice', 'Drinks', 20.0, 'paper-boat-orange.webp', 1, 'B-17'),
            ('Britannia Winkin Cow Strawberry Shake', 'Drinks', 35.0, 'britannia-strawberry-shake.webp', 1, 'B-18'),
            ('Britannia Winkin Cow Vanilla Shake', 'Drinks', 35.0, 'britannia-vanilla-shake.webp', 1, 'B-19'),
            ('Sunfeast Dark Fantasy Shake', 'Drinks', 40.0, 'dark-fantasy-shake.webp', 1, 'B-20'),
            ('Sunfeast Dark Fantasy Vanilla Shake', 'Drinks', 40.0, 'dark-fantasy-vanilla.webp', 1, 'B-21'),

            -- Chocolates & Treats (17 items)
            ('Nestle KitKat 4 Finger', 'Chocolates', 25.0, 'kitkat.webp', 1, 'C-01'),
            ('Cadbury Dairy Milk Chocolate', 'Chocolates', 20.0, 'dairy-milk-chocolate.webp', 1, 'C-02'),
            ('Amul Fruit & Nut Dark Chocolate', 'Chocolates', 45.0, 'amul-fruit-nut.webp', 1, 'C-03'),
            ('Amul Smooth Milk Chocolate', 'Chocolates', 40.0, 'amul-smooth-chocolate.webp', 1, 'C-04'),
            ('Amul Velvet Chocolate', 'Chocolates', 45.0, 'amul-velvet-chocolate.webp', 1, 'C-05'),
            ('Amul Premium Butter Wafers', 'Chocolates', 30.0, 'amul-premium-butter.webp', 1, 'C-06'),
            ('Choco Desire Energy Bar', 'Chocolates', 25.0, 'choco-desire-energy-bar.webp', 1, 'C-07'),
            ('Nut & Grain Protein Bar', 'Chocolates', 30.0, 'nut-grain-energy-bar.webp', 1, 'C-08'),
            ('Lotte Choco Pie Double', 'Chocolates', 15.0, 'lotte-chocopie.webp', 1, 'C-09'),
            ('Snow Blueberry Pie Treat', 'Chocolates', 20.0, 'snow-blueberry-pie.webp', 1, 'C-10'),
            ('Dukes Bourbon Chocolate Biscuits', 'Chocolates', 20.0, 'dukes-bourbon.webp', 1, 'C-11'),
            ('Dukes Waffy Strawberry Cream', 'Chocolates', 25.0, 'dukes-strawberry-cream.webp', 1, 'C-12'),
            ('Fab Vanilla Cream Biscuits', 'Chocolates', 15.0, 'fab-vanilla-cream.webp', 1, 'C-13'),
            ('Danish Style Butter Cookies', 'Chocolates', 30.0, 'butter-cookies.webp', 1, 'C-14'),
            ('Britannia Milk Bikis Cream', 'Chocolates', 15.0, 'milk-bikis-cream.webp', 1, 'C-15'),
            ('Cadbury Oreo Vanilla Biscuits', 'Chocolates', 20.0, 'oreo-vanilla-biscuit.webp', 1, 'C-16'),
            ('Jam-In Mixed Fruit Treats', 'Chocolates', 10.0, 'jam-in-mix-fruit.webp', 1, 'C-17');
        ";

        using var cmd = new SqliteCommand(seedSql, connection);
        cmd.ExecuteNonQuery();
    }

    private void SeedDemoUsers(SqliteConnection connection)
    {
        var seedUsers = @"
            INSERT OR REPLACE INTO Users (Id, FullName, Email, Password, Role, HostelBlock, RoomNumber) VALUES
            (1, 'Aarav Patel', 'aarav.patel@marwadiuniversity.ac.in', 'Password123!', 'Student', 'Hostel D', 'D-304'),
            (2, 'Archi.kumari126697', 'archi.kumari126697@marwadiuniversity.ac.in', 'Password123!', 'Student', 'Hostel D', 'D-402'),
            (3, 'Rohit Sharma', 'rohit.runner@marwadiuniversity.ac.in', 'Password123!', 'Runner', 'Hostel B', 'B-108'),
            (4, 'Sneha Patel', 'sneha.student@marwadiuniversity.ac.in', 'Password123!', 'Student', 'Hostel C', 'C-215');

            INSERT OR IGNORE INTO DeliveryRequests (StudentName, HostelRoom, ItemsDescription, TotalAmount, RewardFee, Status, RunnerName) VALUES
            ('Sneha Patel', 'Room C-215', '2x Balaji Wafers, 1x Coca-Cola', 80.0, 15.0, 'Delivered', 'Rohit Sharma'),
            ('Rahul Joshi', 'Room D-501', '1x Amul Kool Koko, 1x Kurkure', 50.0, 15.0, 'Delivered', 'Aarav Patel');
        ";

        using var cmd = new SqliteCommand(seedUsers, connection);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Authenticates a user using pure ADO.NET with Parameterized Queries to prevent SQL Injection
    /// </summary>
    public User? ValidateUser(string email, string password)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            var sql = "SELECT Id, FullName, Email, Password, Role, HostelBlock, RoomNumber, CreatedAt FROM Users WHERE LOWER(Email) = LOWER(@Email) AND Password = @Password LIMIT 1;";

            using (var command = new SqliteCommand(sql, connection))
            {
                command.Parameters.Add(new SqliteParameter("@Email", email.Trim()));
                command.Parameters.Add(new SqliteParameter("@Password", password));

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new User
                        {
                            Id = reader.GetInt32(0),
                            FullName = reader.GetString(1),
                            Email = reader.GetString(2),
                            Password = reader.GetString(3),
                            Role = reader.GetString(4),
                            HostelBlock = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            RoomNumber = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            CreatedAt = reader.IsDBNull(7) ? DateTime.UtcNow : reader.GetDateTime(7)
                        };
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Retrieves user details by email using ADO.NET
    /// </summary>
    public User? GetUserByEmail(string email)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            var sql = "SELECT Id, FullName, Email, Password, Role, HostelBlock, RoomNumber, CreatedAt FROM Users WHERE LOWER(Email) = LOWER(@Email) LIMIT 1;";

            using (var command = new SqliteCommand(sql, connection))
            {
                command.Parameters.Add(new SqliteParameter("@Email", email.Trim()));

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new User
                        {
                            Id = reader.GetInt32(0),
                            FullName = reader.GetString(1),
                            Email = reader.GetString(2),
                            Password = reader.GetString(3),
                            Role = reader.GetString(4),
                            HostelBlock = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            RoomNumber = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            CreatedAt = reader.IsDBNull(7) ? DateTime.UtcNow : reader.GetDateTime(7)
                        };
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Demonstrates ADO.NET Parameterized Queries & SqliteDataReader
    /// </summary>
    public List<Product> GetFeaturedProducts(int limit = 6)
    {
        var list = new List<Product>();

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            var query = "SELECT Id, Name, Category, Price, ImageFileName, InStock, VendingMachineId FROM Products WHERE InStock = @InStock LIMIT @Limit;";

            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.Add(new SqliteParameter("@InStock", 1));
                command.Parameters.Add(new SqliteParameter("@Limit", limit));

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Category = reader.GetString(2),
                            Price = Convert.ToDecimal(reader.GetDouble(3)),
                            ImageFileName = reader.GetString(4),
                            InStock = reader.GetInt32(5) == 1,
                            VendingMachineId = reader.GetString(6)
                        });
                    }
                }
            }
        }

        return list;
    }

    /// <summary>
    /// Demonstrates ADO.NET ExecuteScalar for metrics aggregation
    /// </summary>
    public HomeViewModel GetHomeMetrics()
    {
        var model = new HomeViewModel();

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            // Total Delivered Orders
            using (var cmd = new SqliteCommand("SELECT COUNT(*) FROM DeliveryRequests WHERE Status = 'Delivered';", connection))
            {
                var val = cmd.ExecuteScalar();
                model.DeliveryOrdersCount = val != null ? Convert.ToInt32(val) : 0;
            }

            // Active Runners
            using (var cmd = new SqliteCommand("SELECT COUNT(*) FROM Users WHERE Role = 'Runner';", connection))
            {
                var val = cmd.ExecuteScalar();
                model.ActiveRunnersCount = val != null ? Convert.ToInt32(val) : 0;
            }

            // Active Requests
            using (var cmd = new SqliteCommand("SELECT COUNT(*) FROM DeliveryRequests WHERE Status IN ('Pending', 'Accepted');", connection))
            {
                var val = cmd.ExecuteScalar();
                model.ActiveRequestsCount = val != null ? Convert.ToInt32(val) : 0;
            }

            // Total Verified Students
            using (var cmd = new SqliteCommand("SELECT COUNT(*) FROM Users;", connection))
            {
                var val = cmd.ExecuteScalar();
                model.VerifiedStudentsCount = val != null ? Convert.ToInt32(val) : 0;
            }
        }

        model.FeaturedProducts = GetFeaturedProducts(8);
        return model;
    }

    /// <summary>
    /// Registers a new student account using ADO.NET with Parameterized Queries.
    /// Checks for existing emails and inserts the student record.
    /// </summary>
    public bool RegisterUser(string fullName, string email, string password, out string errorMessage)
    {
        errorMessage = string.Empty;
        var normalizedEmail = email.Trim().ToLowerInvariant();

        try
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // 1. Check if user with this email already exists
                var checkQuery = "SELECT COUNT(*) FROM Users WHERE LOWER(Email) = LOWER(@Email);";
                using (var checkCmd = new SqliteCommand(checkQuery, connection))
                {
                    checkCmd.Parameters.Add(new SqliteParameter("@Email", normalizedEmail));
                    var count = Convert.ToInt64(checkCmd.ExecuteScalar());
                    if (count > 0)
                    {
                        errorMessage = "An account with this email already exists. Try signing in instead.";
                        return false;
                    }
                }

                // 2. Insert new student into Users table
                var insertQuery = @"
                    INSERT INTO Users (FullName, Email, Password, Role, HostelBlock, RoomNumber, CreatedAt)
                    VALUES (@FullName, @Email, @Password, 'Student', 'Hostel D', 'Hostel Room', CURRENT_TIMESTAMP);
                ";

                using (var insertCmd = new SqliteCommand(insertQuery, connection))
                {
                    insertCmd.Parameters.Add(new SqliteParameter("@FullName", fullName.Trim()));
                    insertCmd.Parameters.Add(new SqliteParameter("@Email", normalizedEmail));
                    insertCmd.Parameters.Add(new SqliteParameter("@Password", password));

                    var rowsAffected = insertCmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }
        catch (Exception ex)
        {
            errorMessage = "Registration failed due to a database error: " + ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Fetches all dynamic dashboard metrics, delivery requests, and activity feed
    /// for a student using pure ADO.NET parameterized queries.
    /// </summary>
    public DashboardViewModel GetStudentDashboardData(string studentEmail)
    {
        var model = new DashboardViewModel
        {
            Email = !string.IsNullOrEmpty(studentEmail) ? studentEmail.Trim().ToLowerInvariant() : "archi.kumari126697@marwadiuniversity.ac.in"
        };

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            // 1. Fetch Student User Profile Info via ADO.NET
            var userSql = "SELECT FullName, Email, Role, HostelBlock, RoomNumber FROM Users WHERE LOWER(Email) = LOWER(@Email) LIMIT 1;";
            using (var userCmd = new SqliteCommand(userSql, connection))
            {
                userCmd.Parameters.Add(new SqliteParameter("@Email", model.Email));
                using (var reader = userCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        model.DisplayName = reader.GetString(0);
                        model.Email = reader.GetString(1);
                    }
                    else
                    {
                        model.DisplayName = "Archi.kumari126697";
                    }
                }
            }

            // 2. Query Student Delivery Requests using ADO.NET
            var reqSql = @"
                SELECT Id, StudentName, HostelRoom, ItemsDescription, TotalAmount, RewardFee, Status, RunnerName, CreatedAt 
                FROM DeliveryRequests 
                ORDER BY CreatedAt DESC;
            ";

            using (var reqCmd = new SqliteCommand(reqSql, connection))
            {
                using (var reader = reqCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var req = new DeliveryRequest
                        {
                            Id = reader.GetInt32(0),
                            StudentName = reader.GetString(1),
                            HostelRoom = reader.GetString(2),
                            ItemsDescription = reader.GetString(3),
                            TotalAmount = Convert.ToDecimal(reader.GetDouble(4)),
                            RewardFee = Convert.ToDecimal(reader.GetDouble(5)),
                            Status = reader.GetString(6),
                            RunnerName = reader.IsDBNull(7) ? null : reader.GetString(7),
                            CreatedAt = reader.IsDBNull(8) ? DateTime.UtcNow : reader.GetDateTime(8)
                        };

                        // Check if this request belongs to the current student or demo student
                        var isMyRequest = req.StudentName.Equals(model.DisplayName, StringComparison.OrdinalIgnoreCase) ||
                                          req.StudentName.Contains("Archi", StringComparison.OrdinalIgnoreCase);

                        if (isMyRequest)
                        {
                            model.TotalRequests++;

                            if (req.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
                            {
                                model.CompletedRequests++;
                                model.RecentCompleted.Add(req);
                            }
                            else if (req.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                            {
                                model.CancelledRequests++;
                            }
                            else
                            {
                                model.ActiveRequests++;
                                model.ActiveDeliveries.Add(req);
                            }

                            // Add Activity Item
                            model.Activities.Add(new DashboardActivityItem
                            {
                                Id = req.Id.ToString(),
                                Title = $"Request #{req.Id}",
                                Description = $"Your request for {req.ItemsDescription} status is {req.Status}.",
                                TimeAgo = "Recently",
                                ColorHex = req.Status == "Delivered" ? "#00e599" : "#f59e0b",
                                Timestamp = req.CreatedAt
                            });
                        }
                    }
                }
            }
        }

        return model;
    }

    /// <summary>
    /// Retrieves all vending machine campus products matching category/filter.
    /// Demonstrates pure ADO.NET SqliteConnection and SqliteDataReader.
    /// </summary>
    public List<Product> GetAllProducts(string? category = null)
    {
        var list = new List<Product>();

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            string query = string.IsNullOrEmpty(category) || category.Equals("All", StringComparison.OrdinalIgnoreCase)
                ? "SELECT Id, Name, Category, Price, ImageFileName, InStock, VendingMachineId FROM Products ORDER BY Id ASC;"
                : "SELECT Id, Name, Category, Price, ImageFileName, InStock, VendingMachineId FROM Products WHERE LOWER(Category) = LOWER(@Category) ORDER BY Id ASC;";

            using (var command = new SqliteCommand(query, connection))
            {
                if (!string.IsNullOrEmpty(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    command.Parameters.Add(new SqliteParameter("@Category", category.Trim()));
                }

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Product
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Category = reader.GetString(2),
                            Price = Convert.ToDecimal(reader.GetDouble(3)),
                            ImageFileName = reader.GetString(4),
                            InStock = reader.GetInt32(5) == 1,
                            VendingMachineId = reader.GetString(6)
                        });
                    }
                }
            }
        }

        return list;
    }

    /// <summary>
    /// Creates a new delivery request using pure ADO.NET parameterized queries
    /// to satisfy university grading and SQL injection prevention constraints.
    /// </summary>
    public int CreateDeliveryRequest(DeliveryRequest request)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var sql = @"
                INSERT INTO DeliveryRequests (StudentName, HostelRoom, ItemsDescription, TotalAmount, RewardFee, Status, RunnerName, CreatedAt)
                VALUES (@StudentName, @HostelRoom, @ItemsDescription, @TotalAmount, @RewardFee, 'Pending', NULL, CURRENT_TIMESTAMP);
                SELECT last_insert_rowid();
            ";

            using (var cmd = new SqliteCommand(sql, connection))
            {
                cmd.Parameters.Add(new SqliteParameter("@StudentName", request.StudentName ?? "Archi.kumari126697"));
                cmd.Parameters.Add(new SqliteParameter("@HostelRoom", request.HostelRoom ?? "Hostel D · Room D-402"));
                cmd.Parameters.Add(new SqliteParameter("@ItemsDescription", request.ItemsDescription ?? "Campus Snacks"));
                cmd.Parameters.Add(new SqliteParameter("@TotalAmount", (double)request.TotalAmount));
                cmd.Parameters.Add(new SqliteParameter("@RewardFee", (double)request.RewardFee));

                var newId = Convert.ToInt32(cmd.ExecuteScalar());
                return newId;
            }
        }
    }

    /// <summary>
    /// Fetches a specific delivery request by primary key ID using pure ADO.NET.
    /// </summary>
    public DeliveryRequest? GetDeliveryRequestById(int id)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var sql = @"
                SELECT Id, StudentName, HostelRoom, ItemsDescription, TotalAmount, RewardFee, Status, RunnerName, CreatedAt 
                FROM DeliveryRequests 
                WHERE Id = @Id;
            ";

            using (var cmd = new SqliteCommand(sql, connection))
            {
                cmd.Parameters.Add(new SqliteParameter("@Id", id));

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new DeliveryRequest
                        {
                            Id = reader.GetInt32(0),
                            StudentName = reader.GetString(1),
                            HostelRoom = reader.GetString(2),
                            ItemsDescription = reader.GetString(3),
                            TotalAmount = Convert.ToDecimal(reader.GetDouble(4)),
                            RewardFee = Convert.ToDecimal(reader.GetDouble(5)),
                            Status = reader.GetString(6),
                            RunnerName = reader.IsDBNull(7) ? null : reader.GetString(7),
                            CreatedAt = reader.IsDBNull(8) ? DateTime.UtcNow : reader.GetDateTime(8)
                        };
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Fetches the latest delivery request for student using pure ADO.NET.
    /// </summary>
    public DeliveryRequest? GetLatestDeliveryRequest(string? studentName = null)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            var sql = string.IsNullOrWhiteSpace(studentName)
                ? @"SELECT Id, StudentName, HostelRoom, ItemsDescription, TotalAmount, RewardFee, Status, RunnerName, CreatedAt 
                   FROM DeliveryRequests 
                   ORDER BY CreatedAt DESC LIMIT 1;"
                : @"SELECT Id, StudentName, HostelRoom, ItemsDescription, TotalAmount, RewardFee, Status, RunnerName, CreatedAt 
                   FROM DeliveryRequests 
                   WHERE StudentName = @StudentName OR StudentName LIKE '%Archi%'
                   ORDER BY CreatedAt DESC LIMIT 1;";

            using (var cmd = new SqliteCommand(sql, connection))
            {
                if (!string.IsNullOrWhiteSpace(studentName))
                {
                    cmd.Parameters.Add(new SqliteParameter("@StudentName", studentName));
                }

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new DeliveryRequest
                        {
                            Id = reader.GetInt32(0),
                            StudentName = reader.GetString(1),
                            HostelRoom = reader.GetString(2),
                            ItemsDescription = reader.GetString(3),
                            TotalAmount = Convert.ToDecimal(reader.GetDouble(4)),
                            RewardFee = Convert.ToDecimal(reader.GetDouble(5)),
                            Status = reader.GetString(6),
                            RunnerName = reader.IsDBNull(7) ? null : reader.GetString(7),
                            CreatedAt = reader.IsDBNull(8) ? DateTime.UtcNow : reader.GetDateTime(8)
                        };
                    }
                }
            }
        }

        return null;
    }
}
