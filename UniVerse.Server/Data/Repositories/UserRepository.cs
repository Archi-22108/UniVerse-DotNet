using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using UniVerse.Server.Models;

namespace UniVerse.Server.Data.Repositories
{
    /// <summary>
    /// User Repository implemented strictly using raw ADO.NET classes.
    /// Demonstrates SqliteConnection, SqliteCommand, SqlParameter, and SqliteDataReader.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly AdoNetDbHelper _db;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(AdoNetDbHelper db, ILogger<UserRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            const string sql = @"
SELECT id, email, password_hash, full_name, enrollment_number, role, hostel_name, room_number, phone_number, is_active_runner, reward_balance, created_at, updated_at
FROM users
WHERE LOWER(email) = LOWER(@email);";

            var param = AdoNetDbHelper.CreateParameter("@email", email.Trim());
            return await _db.ExecuteSingleAsync(sql, MapUserFromReader, param);
        }

        public async Task<User?> GetByIdAsync(string id)
        {
            const string sql = @"
SELECT id, email, password_hash, full_name, enrollment_number, role, hostel_name, room_number, phone_number, is_active_runner, reward_balance, created_at, updated_at
FROM users
WHERE id = @id;";

            var param = AdoNetDbHelper.CreateParameter("@id", id);
            return await _db.ExecuteSingleAsync(sql, MapUserFromReader, param);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            const string sql = @"
SELECT id, email, password_hash, full_name, enrollment_number, role, hostel_name, room_number, phone_number, is_active_runner, reward_balance, created_at, updated_at
FROM users
ORDER BY created_at ASC;";

            return await _db.ExecuteReaderAsync(sql, MapUserFromReader);
        }

        public async Task<List<User>> GetActiveRunnersAsync()
        {
            const string sql = @"
SELECT id, email, password_hash, full_name, enrollment_number, role, hostel_name, room_number, phone_number, is_active_runner, reward_balance, created_at, updated_at
FROM users
WHERE role = 'runner' AND is_active_runner = 1
ORDER BY full_name ASC;";

            return await _db.ExecuteReaderAsync(sql, MapUserFromReader);
        }

        public async Task<int> CreateUserAsync(User user)
        {
            const string sql = @"
INSERT INTO users (id, email, password_hash, full_name, enrollment_number, role, hostel_name, room_number, phone_number, is_active_runner, reward_balance, created_at, updated_at)
VALUES (@id, @email, @password_hash, @full_name, @enrollment_number, @role, @hostel_name, @room_number, @phone_number, @is_active_runner, @reward_balance, @created_at, @updated_at);";

            var parameters = new[]
            {
                AdoNetDbHelper.CreateParameter("@id", user.Id),
                AdoNetDbHelper.CreateParameter("@email", user.Email.Trim().ToLowerInvariant()),
                AdoNetDbHelper.CreateParameter("@password_hash", user.PasswordHash),
                AdoNetDbHelper.CreateParameter("@full_name", user.FullName.Trim()),
                AdoNetDbHelper.CreateParameter("@enrollment_number", user.EnrollmentNumber?.Trim()),
                AdoNetDbHelper.CreateParameter("@role", user.Role.ToLowerInvariant()),
                AdoNetDbHelper.CreateParameter("@hostel_name", user.HostelName?.Trim()),
                AdoNetDbHelper.CreateParameter("@room_number", user.RoomNumber?.Trim()),
                AdoNetDbHelper.CreateParameter("@phone_number", user.PhoneNumber?.Trim()),
                AdoNetDbHelper.CreateParameter("@is_active_runner", user.IsActiveRunner ? 1 : 0),
                AdoNetDbHelper.CreateParameter("@reward_balance", user.RewardBalance),
                AdoNetDbHelper.CreateParameter("@created_at", user.CreatedAt),
                AdoNetDbHelper.CreateParameter("@updated_at", user.UpdatedAt)
            };

            return await _db.ExecuteNonQueryAsync(sql, parameters);
        }

        public async Task<int> ToggleRunnerDutyAsync(string runnerId, bool isActive)
        {
            const string sql = @"
UPDATE users
SET is_active_runner = @isActive, updated_at = @updated_at
WHERE id = @id AND role = 'runner';";

            var parameters = new[]
            {
                AdoNetDbHelper.CreateParameter("@id", runnerId),
                AdoNetDbHelper.CreateParameter("@isActive", isActive ? 1 : 0),
                AdoNetDbHelper.CreateParameter("@updated_at", DateTime.UtcNow.ToString("o"))
            };

            return await _db.ExecuteNonQueryAsync(sql, parameters);
        }

        public async Task<int> AddRewardBalanceAsync(string userId, double amount)
        {
            const string sql = @"
UPDATE users
SET reward_balance = reward_balance + @amount, updated_at = @updated_at
WHERE id = @id;";

            var parameters = new[]
            {
                AdoNetDbHelper.CreateParameter("@id", userId),
                AdoNetDbHelper.CreateParameter("@amount", amount),
                AdoNetDbHelper.CreateParameter("@updated_at", DateTime.UtcNow.ToString("o"))
            };

            return await _db.ExecuteNonQueryAsync(sql, parameters);
        }

        private static User MapUserFromReader(SqliteDataReader reader)
        {
            return new User
            {
                Id = reader.GetString(0),
                Email = reader.GetString(1),
                PasswordHash = reader.GetString(2),
                FullName = reader.GetString(3),
                EnrollmentNumber = reader.IsDBNull(4) ? null : reader.GetString(4),
                Role = reader.GetString(5),
                HostelName = reader.IsDBNull(6) ? null : reader.GetString(6),
                RoomNumber = reader.IsDBNull(7) ? null : reader.GetString(7),
                PhoneNumber = reader.IsDBNull(8) ? null : reader.GetString(8),
                IsActiveRunner = reader.GetInt32(9) == 1,
                RewardBalance = reader.GetDouble(10),
                CreatedAt = reader.GetString(11),
                UpdatedAt = reader.GetString(12)
            };
        }
    }
}
