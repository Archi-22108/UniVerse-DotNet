using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using UniVerse.Server.Models;

namespace UniVerse.Server.Data.Repositories
{
    /// <summary>
    /// Delivery Request and Runner Hub Repository implemented strictly using raw ADO.NET.
    /// Demonstrates complex JOIN queries, parameterized filters, and multi-table transactions.
    /// </summary>
    public class DeliveryRepository : IDeliveryRepository
    {
        private readonly AdoNetDbHelper _db;
        private readonly ILogger<DeliveryRepository> _logger;

        public DeliveryRepository(AdoNetDbHelper db, ILogger<DeliveryRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<DeliveryRequestDetailDto>> GetRequestsAsync(string? status = null)
        {
            string sql = @"
SELECT 
    r.id, r.requester_id, u_req.full_name AS requester_name, u_req.phone_number AS requester_phone,
    r.runner_id, u_run.full_name AS runner_name, u_run.phone_number AS runner_phone,
    r.pickup_location, r.dropoff_location, r.instructions, r.total_estimated_amount, r.delivery_fee,
    r.status, r.delivery_otp, r.created_at, r.updated_at
FROM delivery_requests r
INNER JOIN users u_req ON r.requester_id = u_req.id
LEFT JOIN users u_run ON r.runner_id = u_run.id ";

            SqliteParameter[] parameters;
            if (!string.IsNullOrEmpty(status))
            {
                sql += " WHERE LOWER(r.status) = LOWER(@status) ";
                parameters = new[] { AdoNetDbHelper.CreateParameter("@status", status) };
            }
            else
            {
                parameters = Array.Empty<SqliteParameter>();
            }

            sql += " ORDER BY r.created_at DESC;";

            var requests = await _db.ExecuteReaderAsync(sql, MapDeliveryFromReader, parameters);
            await PopulateRequestItemsAsync(requests);
            return requests;
        }

        public async Task<DeliveryRequestDetailDto?> GetRequestByIdAsync(string id)
        {
            const string sql = @"
SELECT 
    r.id, r.requester_id, u_req.full_name AS requester_name, u_req.phone_number AS requester_phone,
    r.runner_id, u_run.full_name AS runner_name, u_run.phone_number AS runner_phone,
    r.pickup_location, r.dropoff_location, r.instructions, r.total_estimated_amount, r.delivery_fee,
    r.status, r.delivery_otp, r.created_at, r.updated_at
FROM delivery_requests r
INNER JOIN users u_req ON r.requester_id = u_req.id
LEFT JOIN users u_run ON r.runner_id = u_run.id
WHERE r.id = @id;";

            var param = AdoNetDbHelper.CreateParameter("@id", id);
            var req = await _db.ExecuteSingleAsync(sql, MapDeliveryFromReader, param);
            if (req != null)
            {
                req.Items = await GetItemsForRequestAsync(req.Id);
            }
            return req;
        }

        public async Task<List<DeliveryRequestDetailDto>> GetRequestsByRequesterAsync(string requesterId)
        {
            const string sql = @"
SELECT 
    r.id, r.requester_id, u_req.full_name AS requester_name, u_req.phone_number AS requester_phone,
    r.runner_id, u_run.full_name AS runner_name, u_run.phone_number AS runner_phone,
    r.pickup_location, r.dropoff_location, r.instructions, r.total_estimated_amount, r.delivery_fee,
    r.status, r.delivery_otp, r.created_at, r.updated_at
FROM delivery_requests r
INNER JOIN users u_req ON r.requester_id = u_req.id
LEFT JOIN users u_run ON r.runner_id = u_run.id
WHERE r.requester_id = @requesterId
ORDER BY r.created_at DESC;";

            var param = AdoNetDbHelper.CreateParameter("@requesterId", requesterId);
            var requests = await _db.ExecuteReaderAsync(sql, MapDeliveryFromReader, param);
            await PopulateRequestItemsAsync(requests);
            return requests;
        }

        public async Task<List<DeliveryRequestDetailDto>> GetRequestsByRunnerAsync(string runnerId)
        {
            const string sql = @"
SELECT 
    r.id, r.requester_id, u_req.full_name AS requester_name, u_req.phone_number AS requester_phone,
    r.runner_id, u_run.full_name AS runner_name, u_run.phone_number AS runner_phone,
    r.pickup_location, r.dropoff_location, r.instructions, r.total_estimated_amount, r.delivery_fee,
    r.status, r.delivery_otp, r.created_at, r.updated_at
FROM delivery_requests r
INNER JOIN users u_req ON r.requester_id = u_req.id
LEFT JOIN users u_run ON r.runner_id = u_run.id
WHERE r.runner_id = @runnerId
ORDER BY r.created_at DESC;";

            var param = AdoNetDbHelper.CreateParameter("@runnerId", runnerId);
            var requests = await _db.ExecuteReaderAsync(sql, MapDeliveryFromReader, param);
            await PopulateRequestItemsAsync(requests);
            return requests;
        }

        public async Task<string> CreateRequestAsync(DeliveryRequest request, List<RequestItem> items)
        {
            var requestId = Guid.NewGuid().ToString();
            var now = DateTime.UtcNow.ToString("o");
            var otp = new Random().Next(100000, 999999).ToString(); // 6-digit OTP per syllabus requirement

            await _db.ExecuteTransactionAsync(async (conn, trans) =>
            {
                const string insertReq = @"
INSERT INTO delivery_requests (id, requester_id, runner_id, pickup_location, dropoff_location, instructions, total_estimated_amount, delivery_fee, status, delivery_otp, created_at, updated_at)
VALUES (@id, @requester_id, NULL, @pickup_location, @dropoff_location, @instructions, @total_estimated_amount, @delivery_fee, 'pending', @delivery_otp, @created_at, @updated_at);";

                await using (var cmd = new SqliteCommand(insertReq, conn, trans))
                {
                    cmd.Parameters.AddWithValue("@id", requestId);
                    cmd.Parameters.AddWithValue("@requester_id", request.RequesterId);
                    cmd.Parameters.AddWithValue("@pickup_location", request.PickupLocation);
                    cmd.Parameters.AddWithValue("@dropoff_location", request.DropoffLocation);
                    cmd.Parameters.AddWithValue("@instructions", (object?)request.Instructions ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@total_estimated_amount", request.TotalEstimatedAmount);
                    cmd.Parameters.AddWithValue("@delivery_fee", request.DeliveryFee);
                    cmd.Parameters.AddWithValue("@delivery_otp", otp);
                    cmd.Parameters.AddWithValue("@created_at", now);
                    cmd.Parameters.AddWithValue("@updated_at", now);
                    await cmd.ExecuteNonQueryAsync();
                }

                if (items != null && items.Count > 0)
                {
                    const string insertItem = @"
INSERT INTO request_items (id, request_id, name, quantity, notes, estimated_price)
VALUES (@id, @request_id, @name, @quantity, @notes, @estimated_price);";

                    foreach (var itm in items)
                    {
                        await using var itemCmd = new SqliteCommand(insertItem, conn, trans);
                        itemCmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
                        itemCmd.Parameters.AddWithValue("@request_id", requestId);
                        itemCmd.Parameters.AddWithValue("@name", itm.Name);
                        itemCmd.Parameters.AddWithValue("@quantity", itm.Quantity);
                        itemCmd.Parameters.AddWithValue("@notes", (object?)itm.Notes ?? DBNull.Value);
                        itemCmd.Parameters.AddWithValue("@estimated_price", itm.EstimatedPrice);
                        await itemCmd.ExecuteNonQueryAsync();
                    }
                }
            });

            return requestId;
        }

        public async Task<bool> AssignRunnerAsync(string requestId, string runnerId)
        {
            const string sql = @"
UPDATE delivery_requests
SET runner_id = @runnerId, status = 'accepted', updated_at = @updated_at
WHERE id = @requestId AND status = 'pending';";

            var parameters = new[]
            {
                AdoNetDbHelper.CreateParameter("@requestId", requestId),
                AdoNetDbHelper.CreateParameter("@runnerId", runnerId),
                AdoNetDbHelper.CreateParameter("@updated_at", DateTime.UtcNow.ToString("o"))
            };

            int rows = await _db.ExecuteNonQueryAsync(sql, parameters);
            return rows > 0;
        }

        public async Task<bool> UpdateStatusAsync(string requestId, string status)
        {
            const string sql = @"
UPDATE delivery_requests
SET status = @status, updated_at = @updated_at
WHERE id = @id;";

            var parameters = new[]
            {
                AdoNetDbHelper.CreateParameter("@id", requestId),
                AdoNetDbHelper.CreateParameter("@status", status.ToLowerInvariant()),
                AdoNetDbHelper.CreateParameter("@updated_at", DateTime.UtcNow.ToString("o"))
            };

            int rows = await _db.ExecuteNonQueryAsync(sql, parameters);
            return rows > 0;
        }

        public async Task<(bool Success, string Message, double Reward)> CompleteDeliveryWithOtpAsync(string requestId, string runnerId, string otp)
        {
            const string selectSql = "SELECT runner_id, delivery_fee, delivery_otp, status FROM delivery_requests WHERE id = @id;";
            var req = await _db.ExecuteSingleAsync(selectSql, reader => new
            {
                RunnerId = reader.IsDBNull(0) ? null : reader.GetString(0),
                DeliveryFee = reader.GetDouble(1),
                DeliveryOtp = reader.IsDBNull(2) ? null : reader.GetString(2),
                Status = reader.GetString(3)
            }, AdoNetDbHelper.CreateParameter("@id", requestId));

            if (req == null)
            {
                return (false, "Delivery order not found.", 0);
            }

            if (req.RunnerId != runnerId)
            {
                return (false, "You are not the designated runner for this order.", 0);
            }

            if (string.Compare(req.DeliveryOtp?.Trim(), otp.Trim(), StringComparison.OrdinalIgnoreCase) != 0)
            {
                return (false, "Invalid delivery confirmation OTP. Ask student for 6-digit code.", 0);
            }

            var now = DateTime.UtcNow.ToString("o");

            await _db.ExecuteTransactionAsync(async (conn, trans) =>
            {
                const string updateReq = "UPDATE delivery_requests SET status = 'delivered', updated_at = @updated_at WHERE id = @id;";
                await using (var cmd = new SqliteCommand(updateReq, conn, trans))
                {
                    cmd.Parameters.AddWithValue("@id", requestId);
                    cmd.Parameters.AddWithValue("@updated_at", now);
                    await cmd.ExecuteNonQueryAsync();
                }

                const string creditRunner = "UPDATE users SET reward_balance = reward_balance + @fee, updated_at = @updated_at WHERE id = @runnerId;";
                await using (var cmd2 = new SqliteCommand(creditRunner, conn, trans))
                {
                    cmd2.Parameters.AddWithValue("@runnerId", runnerId);
                    cmd2.Parameters.AddWithValue("@fee", req.DeliveryFee);
                    cmd2.Parameters.AddWithValue("@updated_at", now);
                    await cmd2.ExecuteNonQueryAsync();
                }
            });

            return (true, "Order delivered successfully! Tip credited to runner balance.", req.DeliveryFee);
        }

        private async Task<List<RequestItem>> GetItemsForRequestAsync(string requestId)
        {
            const string sql = "SELECT id, request_id, name, quantity, notes, estimated_price FROM request_items WHERE request_id = @requestId;";
            var param = AdoNetDbHelper.CreateParameter("@requestId", requestId);
            return await _db.ExecuteReaderAsync(sql, reader => new RequestItem
            {
                Id = reader.GetString(0),
                RequestId = reader.GetString(1),
                Name = reader.GetString(2),
                Quantity = reader.GetInt32(3),
                Notes = reader.IsDBNull(4) ? null : reader.GetString(4),
                EstimatedPrice = reader.GetDouble(5)
            }, param);
        }

        private async Task PopulateRequestItemsAsync(List<DeliveryRequestDetailDto> requests)
        {
            if (requests == null || requests.Count == 0) return;

            const string allItemsSql = "SELECT id, request_id, name, quantity, notes, estimated_price FROM request_items;";
            var allItems = await _db.ExecuteReaderAsync(allItemsSql, reader => new RequestItem
            {
                Id = reader.GetString(0),
                RequestId = reader.GetString(1),
                Name = reader.GetString(2),
                Quantity = reader.GetInt32(3),
                Notes = reader.IsDBNull(4) ? null : reader.GetString(4),
                EstimatedPrice = reader.GetDouble(5)
            });

            var grouped = allItems.GroupBy(i => i.RequestId).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var r in requests)
            {
                if (grouped.TryGetValue(r.Id, out var items))
                {
                    r.Items = items;
                }
            }
        }

        private static DeliveryRequestDetailDto MapDeliveryFromReader(SqliteDataReader reader)
        {
            return new DeliveryRequestDetailDto
            {
                Id = reader.GetString(0),
                RequesterId = reader.GetString(1),
                RequesterName = reader.GetString(2),
                RequesterPhone = reader.IsDBNull(3) ? null : reader.GetString(3),
                RunnerId = reader.IsDBNull(4) ? null : reader.GetString(4),
                RunnerName = reader.IsDBNull(5) ? null : reader.GetString(5),
                RunnerPhone = reader.IsDBNull(6) ? null : reader.GetString(6),
                PickupLocation = reader.GetString(7),
                DropoffLocation = reader.GetString(8),
                Instructions = reader.IsDBNull(9) ? null : reader.GetString(9),
                TotalEstimatedAmount = reader.GetDouble(10),
                DeliveryFee = reader.GetDouble(11),
                Status = reader.GetString(12),
                DeliveryOtp = reader.IsDBNull(13) ? null : reader.GetString(13),
                CreatedAt = reader.GetString(14),
                UpdatedAt = reader.GetString(15)
            };
        }
    }
}
