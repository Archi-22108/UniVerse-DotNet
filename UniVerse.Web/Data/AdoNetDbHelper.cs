using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using UniVerse.Web.Models;

namespace UniVerse.Web.Data;

/// <summary>
/// ADO.NET Data Access Layer (DAL)
/// Implements database operations using raw ADO.NET objects:
/// - SqliteConnection
/// - SqliteCommand
/// - SqliteParameter (SQL Injection Prevention)
/// - SqliteDataReader
/// </summary>
public class AdoNetDbHelper
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

            // Seed sample products if empty
            var checkCountQuery = "SELECT COUNT(*) FROM Products;";
            using (var countCmd = new SqliteCommand(checkCountQuery, connection))
            {
                var count = Convert.ToInt64(countCmd.ExecuteScalar());
                if (count == 0)
                {
                    SeedProducts(connection);
                    SeedSampleData(connection);
                }
            }
        }
    }

    private void SeedProducts(SqliteConnection connection)
    {
        var seedSql = @"
            INSERT INTO Products (Name, Category, Price, ImageFileName, InStock, VendingMachineId) VALUES
            ('Balaji Masala Wafers', 'Snacks', 20.0, 'balaji-masala-wafers.webp', 1, 'VM-HOSTEL-D'),
            ('Coca-Cola Can 250ml', 'Drinks', 40.0, 'coca-cola-can.webp', 1, 'VM-HOSTEL-D'),
            ('Amul Kool Koko', 'Beverages', 30.0, 'amul-kool-koko.webp', 1, 'VM-HOSTEL-D'),
            ('Kurkure Masala Munch', 'Snacks', 20.0, 'kurkure-masala-munch.webp', 1, 'VM-HOSTEL-D'),
            ('Lay''s Magic Masala', 'Snacks', 20.0, 'lays-magic-masala.webp', 1, 'VM-HOSTEL-D'),
            ('Kinley Water Bottle 500ml', 'Drinks', 10.0, 'kinley-water-500ml.webp', 1, 'VM-HOSTEL-D'),
            ('Doritos Cheese Supreme', 'Snacks', 30.0, 'doritos-cheese.webp', 1, 'VM-HOSTEL-D'),
            ('Act II Butter Popcorn', 'Snacks', 35.0, 'act-butter-popcorn.webp', 1, 'VM-HOSTEL-D'),
            ('Frooti Mango 400ml', 'Drinks', 20.0, 'frooti-400ml.webp', 1, 'VM-HOSTEL-D'),
            ('Maggi 2-Minute Noodles', 'Snacks', 15.0, 'maggi-2-min.webp', 1, 'VM-HOSTEL-D');
        ";

        using var cmd = new SqliteCommand(seedSql, connection);
        cmd.ExecuteNonQuery();
    }

    private void SeedSampleData(SqliteConnection connection)
    {
        var seedUsers = @"
            INSERT OR IGNORE INTO Users (FullName, Email, Role, HostelBlock, RoomNumber) VALUES
            ('Archi Kumari', 'archi.student@marwadiuniversity.ac.in', 'Student', 'Hostel D', 'D-402'),
            ('Rohit Sharma', 'rohit.runner@marwadiuniversity.ac.in', 'Runner', 'Hostel B', 'B-108'),
            ('Sneha Patel', 'sneha.student@marwadiuniversity.ac.in', 'Student', 'Hostel C', 'C-215'),
            ('Aman Verma', 'aman.runner@marwadiuniversity.ac.in', 'Runner', 'Hostel D', 'D-310');

            INSERT INTO DeliveryRequests (StudentName, HostelRoom, ItemsDescription, TotalAmount, RewardFee, Status, RunnerName) VALUES
            ('Sneha Patel', 'Room C-215', '2x Balaji Wafers, 1x Coca-Cola', 80.0, 15.0, 'Delivered', 'Rohit Sharma'),
            ('Rahul Joshi', 'Room D-501', '1x Amul Kool Koko, 1x Kurkure', 50.0, 15.0, 'Delivered', 'Aman Verma');
        ";

        using var cmd = new SqliteCommand(seedUsers, connection);
        cmd.ExecuteNonQuery();
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
                // ADO.NET Parameters to prevent SQL Injection
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
}
