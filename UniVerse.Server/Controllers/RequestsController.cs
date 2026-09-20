using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using UniVerse.Server.Data;
using UniVerse.Server.Models;

namespace UniVerse.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class RequestsController : ControllerBase
    {
        private readonly AdoNetDbHelper _dbHelper;
        private readonly ILogger<RequestsController> _logger;

        public RequestsController(AdoNetDbHelper dbHelper, ILogger<RequestsController> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves campus delivery requests, optionally filtered by status (e.g. pending, in_transit).
        /// Uses ADO.NET ExecuteReaderAsync.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<DeliveryRequestDetailDto>>> GetRequests([FromQuery] string? status)
        {
            string querySql = @"
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
                querySql += " WHERE r.status = @status ";
                parameters = new[] { AdoNetDbHelper.CreateParameter("@status", status.ToLowerInvariant()) };
            }
            else
            {
                parameters = Array.Empty<SqliteParameter>();
            }

            querySql += " ORDER BY r.created_at DESC;";

            var requests = await _dbHelper.ExecuteReaderAsync(querySql, reader => new DeliveryRequestDetailDto
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
            }, parameters);

            // Fetch items for each request using ADO.NET
            if (requests.Count > 0)
            {
                const string itemsSql = "SELECT id, request_id, name, quantity, notes, estimated_price FROM request_items;";
                var allItems = await _dbHelper.ExecuteReaderAsync(itemsSql, reader => new RequestItem
                {
                    Id = reader.GetString(0),
                    RequestId = reader.GetString(1),
                    Name = reader.GetString(2),
                    Quantity = reader.GetInt32(3),
                    Notes = reader.IsDBNull(4) ? null : reader.GetString(4),
                    EstimatedPrice = reader.GetDouble(5)
                });

                var itemsByRequestId = allItems.GroupBy(i => i.RequestId).ToDictionary(g => g.Key, g => g.ToList());
                foreach (var req in requests)
                {
                    if (itemsByRequestId.TryGetValue(req.Id, out var items))
                    {
                        req.Items = items;
                    }
                }
            }

            return Ok(requests);
        }

        /// <summary>
        /// Retrieves a single delivery request by its ID using ADO.NET.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<DeliveryRequestDetailDto>> GetRequestById(string id)
        {
            const string querySql = @"
SELECT 
    r.id, r.requester_id, u_req.full_name AS requester_name, u_req.phone_number AS requester_phone,
    r.runner_id, u_run.full_name AS runner_name, u_run.phone_number AS runner_phone,
    r.pickup_location, r.dropoff_location, r.instructions, r.total_estimated_amount, r.delivery_fee,
    r.status, r.delivery_otp, r.created_at, r.updated_at
FROM delivery_requests r
INNER JOIN users u_req ON r.requester_id = u_req.id
LEFT JOIN users u_run ON r.runner_id = u_run.id
WHERE r.id = @id;";

            var paramId = AdoNetDbHelper.CreateParameter("@id", id);
            var req = await _dbHelper.ExecuteSingleAsync(querySql, reader => new DeliveryRequestDetailDto
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
            }, paramId);

            if (req == null)
            {
                return NotFound(new { message = $"Delivery request {id} not found." });
            }

            const string itemsSql = "SELECT id, request_id, name, quantity, notes, estimated_price FROM request_items WHERE request_id = @requestId;";
            var paramReqId = AdoNetDbHelper.CreateParameter("@requestId", id);
            req.Items = await _dbHelper.ExecuteReaderAsync(itemsSql, reader => new RequestItem
            {
                Id = reader.GetString(0),
                RequestId = reader.GetString(1),
                Name = reader.GetString(2),
                Quantity = reader.GetInt32(3),
                Notes = reader.IsDBNull(4) ? null : reader.GetString(4),
                EstimatedPrice = reader.GetDouble(5)
            }, paramReqId);

            return Ok(req);
        }

        /// <summary>
        /// Creates a new delivery request and its items inside an atomic ADO.NET transaction.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<DeliveryRequestDetailDto>> CreateRequest([FromBody] CreateDeliveryRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var requestId = Guid.NewGuid().ToString();
            var now = DateTime.UtcNow.ToString("o");
            var otp = new Random().Next(1000, 9999).ToString();
            double totalItemsPrice = dto.Items?.Sum(i => i.EstimatedPrice * i.Quantity) ?? 0.0;

            await _dbHelper.ExecuteTransactionAsync(async (conn, trans) =>
            {
                // 1. Insert Request
                const string insertReqSql = @"
INSERT INTO delivery_requests (id, requester_id, runner_id, pickup_location, dropoff_location, instructions, total_estimated_amount, delivery_fee, status, delivery_otp, created_at, updated_at)
VALUES (@id, @requester_id, NULL, @pickup_location, @dropoff_location, @instructions, @total_estimated_amount, @delivery_fee, 'pending', @delivery_otp, @created_at, @updated_at);";

                await using (var cmd = new SqliteCommand(insertReqSql, conn, trans))
                {
                    cmd.Parameters.AddWithValue("@id", requestId);
                    cmd.Parameters.AddWithValue("@requester_id", dto.RequesterId);
                    cmd.Parameters.AddWithValue("@pickup_location", dto.PickupLocation);
                    cmd.Parameters.AddWithValue("@dropoff_location", dto.DropoffLocation);
                    cmd.Parameters.AddWithValue("@instructions", (object?)dto.Instructions ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@total_estimated_amount", totalItemsPrice);
                    cmd.Parameters.AddWithValue("@delivery_fee", dto.DeliveryFee);
                    cmd.Parameters.AddWithValue("@delivery_otp", otp);
                    cmd.Parameters.AddWithValue("@created_at", now);
                    cmd.Parameters.AddWithValue("@updated_at", now);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 2. Insert Items
                if (dto.Items != null && dto.Items.Count > 0)
                {
                    const string insertItemSql = @"
INSERT INTO request_items (id, request_id, name, quantity, notes, estimated_price)
VALUES (@id, @request_id, @name, @quantity, @notes, @estimated_price);";

                    foreach (var item in dto.Items)
                    {
                        await using var itemCmd = new SqliteCommand(insertItemSql, conn, trans);
                        itemCmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
                        itemCmd.Parameters.AddWithValue("@request_id", requestId);
                        itemCmd.Parameters.AddWithValue("@name", item.Name);
                        itemCmd.Parameters.AddWithValue("@quantity", item.Quantity);
                        itemCmd.Parameters.AddWithValue("@notes", (object?)item.Notes ?? DBNull.Value);
                        itemCmd.Parameters.AddWithValue("@estimated_price", item.EstimatedPrice);
                        await itemCmd.ExecuteNonQueryAsync();
                    }
                }
            });

            return await GetRequestById(requestId);
        }

        /// <summary>
        /// Updates the status of a delivery request using ADO.NET.
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateDeliveryStatusDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var now = DateTime.UtcNow.ToString("o");
            const string updateSql = @"
UPDATE delivery_requests
SET status = @status, updated_at = @updated_at
WHERE id = @id;";

            var parameters = new[]
            {
                AdoNetDbHelper.CreateParameter("@id", id),
                AdoNetDbHelper.CreateParameter("@status", dto.Status.ToLowerInvariant()),
                AdoNetDbHelper.CreateParameter("@updated_at", now)
            };

            int affected = await _dbHelper.ExecuteNonQueryAsync(updateSql, parameters);
            if (affected == 0)
            {
                return NotFound(new { message = "Request not found." });
            }

            return Ok(new { message = $"Status updated to {dto.Status}." });
        }
    }
}
