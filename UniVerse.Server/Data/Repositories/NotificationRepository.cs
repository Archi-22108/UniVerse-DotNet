using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using UniVerse.Server.Models;

namespace UniVerse.Server.Data.Repositories
{
    /// <summary>
    /// Notification Repository implemented strictly using raw ADO.NET classes.
    /// Demonstrates SqliteConnection, SqliteCommand, SqliteParameter, and SqliteDataReader.
    /// </summary>
    public class NotificationRepository : INotificationRepository
    {
        private readonly AdoNetDbHelper _db;
        private readonly ILogger<NotificationRepository> _logger;

        private const string NotificationSelectFields = @"id, user_id, title, message, type, reference_id, is_read, created_at";

        public NotificationRepository(AdoNetDbHelper db, ILogger<NotificationRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<Notification>> GetByUserIdAsync(string userId, int limit = 30)
        {
            string sql = $@"
SELECT {NotificationSelectFields}
FROM notifications
WHERE user_id = @userId
ORDER BY created_at DESC
LIMIT @limit;";

            var pUserId = AdoNetDbHelper.CreateParameter("@userId", userId);
            var pLimit = AdoNetDbHelper.CreateParameter("@limit", limit);

            return await _db.ExecuteReaderAsync(sql, MapNotificationFromReader, pUserId, pLimit);
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            string sql = @"
SELECT COUNT(*)
FROM notifications
WHERE user_id = @userId AND is_read = 0;";

            var pUserId = AdoNetDbHelper.CreateParameter("@userId", userId);
            long count = await _db.ExecuteScalarAsync<long>(sql, pUserId);
            return (int)count;
        }

        public async Task<int> MarkAsReadAsync(string id, string userId)
        {
            string sql = @"
UPDATE notifications
SET is_read = 1
WHERE id = @id AND user_id = @userId;";

            var pId = AdoNetDbHelper.CreateParameter("@id", id);
            var pUserId = AdoNetDbHelper.CreateParameter("@userId", userId);

            return await _db.ExecuteNonQueryAsync(sql, pId, pUserId);
        }

        public async Task<int> MarkAllAsReadAsync(string userId)
        {
            string sql = @"
UPDATE notifications
SET is_read = 1
WHERE user_id = @userId AND is_read = 0;";

            var pUserId = AdoNetDbHelper.CreateParameter("@userId", userId);
            return await _db.ExecuteNonQueryAsync(sql, pUserId);
        }

        public async Task<int> DeleteAsync(string id, string userId)
        {
            string sql = @"
DELETE FROM notifications
WHERE id = @id AND user_id = @userId;";

            var pId = AdoNetDbHelper.CreateParameter("@id", id);
            var pUserId = AdoNetDbHelper.CreateParameter("@userId", userId);

            return await _db.ExecuteNonQueryAsync(sql, pId, pUserId);
        }

        public async Task<int> ClearAllAsync(string userId)
        {
            string sql = @"
DELETE FROM notifications
WHERE user_id = @userId;";

            var pUserId = AdoNetDbHelper.CreateParameter("@userId", userId);
            return await _db.ExecuteNonQueryAsync(sql, pUserId);
        }

        public async Task<int> CreateAsync(Notification notification)
        {
            string sql = @"
INSERT INTO notifications (id, user_id, title, message, type, reference_id, is_read, created_at)
VALUES (@id, @userId, @title, @message, @type, @referenceId, @isRead, @createdAt);";

            var pId = AdoNetDbHelper.CreateParameter("@id", notification.Id);
            var pUserId = AdoNetDbHelper.CreateParameter("@userId", notification.UserId);
            var pTitle = AdoNetDbHelper.CreateParameter("@title", notification.Title);
            var pMessage = AdoNetDbHelper.CreateParameter("@message", notification.Message);
            var pType = AdoNetDbHelper.CreateParameter("@type", notification.Type);
            var pRefId = AdoNetDbHelper.CreateParameter("@referenceId", (object?)notification.ReferenceId ?? DBNull.Value);
            var pIsRead = AdoNetDbHelper.CreateParameter("@isRead", notification.IsRead ? 1 : 0);
            var pCreatedAt = AdoNetDbHelper.CreateParameter("@createdAt", notification.CreatedAt);

            return await _db.ExecuteNonQueryAsync(sql, pId, pUserId, pTitle, pMessage, pType, pRefId, pIsRead, pCreatedAt);
        }

        private static Notification MapNotificationFromReader(SqliteDataReader reader)
        {
            return new Notification
            {
                Id = reader.GetString(0),
                UserId = reader.GetString(1),
                Title = reader.GetString(2),
                Message = reader.GetString(3),
                Type = reader.IsDBNull(4) ? "system" : reader.GetString(4),
                ReferenceId = reader.IsDBNull(5) ? null : reader.GetString(5),
                IsRead = !reader.IsDBNull(6) && reader.GetInt32(6) == 1,
                CreatedAt = reader.IsDBNull(7) ? DateTime.UtcNow.ToString("o") : reader.GetString(7)
            };
        }
    }
}
