using System;
using System.Collections.Generic;
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
    public class RunnerController : ControllerBase
    {
        private readonly AdoNetDbHelper _dbHelper;
        private readonly ILogger<RunnerController> _logger;

        public RunnerController(AdoNetDbHelper dbHelper, ILogger<RunnerController> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
        }

        /// <summary>
        /// Toggles a runner's active on-duty status using ADO.NET.
        /// </summary>
        [HttpPost("toggle-duty")]
        public async Task<IActionResult> ToggleDuty([FromBody] ToggleRunnerDutyDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var now = DateTime.UtcNow.ToString("o");
            const string updateSql = @"
UPDATE users
SET is_active_runner = @isActive, updated_at = @updated_at
WHERE id = @id AND role = 'runner';";

            var parameters = new[]
            {
                AdoNetDbHelper.CreateParameter("@id", dto.RunnerId),
                AdoNetDbHelper.CreateParameter("@isActive", dto.IsActive ? 1 : 0),
                AdoNetDbHelper.CreateParameter("@updated_at", now)
            };

            int affected = await _dbHelper.ExecuteNonQueryAsync(updateSql, parameters);
            if (affected == 0)
            {
                return NotFound(new { message = "Runner not found or user is not a runner." });
            }

            return Ok(new { message = $"Runner duty status set to {(dto.IsActive ? "ACTIVE" : "OFF-DUTY")}." });
        }

        /// <summary>
        /// Retrieves all currently active on-duty campus runners using ADO.NET.
        /// </summary>
        [HttpGet("available")]
        public async Task<ActionResult<List<UserDto>>> GetAvailableRunners()
        {
            const string querySql = @"
SELECT id, email, full_name, enrollment_number, role, hostel_name, room_number, phone_number, is_active_runner, reward_balance, created_at
FROM users
WHERE role = 'runner' AND is_active_runner = 1;";

            var runners = await _dbHelper.ExecuteReaderAsync(querySql, reader => new UserDto
            {
                Id = reader.GetString(0),
                Email = reader.GetString(1),
                FullName = reader.GetString(2),
                EnrollmentNumber = reader.IsDBNull(3) ? null : reader.GetString(3),
                Role = reader.GetString(4),
                HostelName = reader.IsDBNull(5) ? null : reader.GetString(5),
                RoomNumber = reader.IsDBNull(6) ? null : reader.GetString(6),
                PhoneNumber = reader.IsDBNull(7) ? null : reader.GetString(7),
                IsActiveRunner = reader.GetInt32(8) == 1,
                RewardBalance = reader.GetDouble(9),
                CreatedAt = reader.GetString(10)
            });

            return Ok(runners);
        }

        /// <summary>
        /// Assigns an on-duty runner to a pending delivery request using ADO.NET.
        /// </summary>
        [HttpPost("accept/{requestId}")]
        public async Task<IActionResult> AcceptDelivery(string requestId, [FromBody] AcceptDeliveryDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verify runner exists and is active
            const string checkRunnerSql = "SELECT COUNT(*) FROM users WHERE id = @id AND role = 'runner' AND is_active_runner = 1;";
            long runnerActive = await _dbHelper.ExecuteScalarAsync<long>(checkRunnerSql, AdoNetDbHelper.CreateParameter("@id", dto.RunnerId));
            if (runnerActive == 0)
            {
                return BadRequest(new { message = "Runner is not currently active on duty." });
            }

            var now = DateTime.UtcNow.ToString("o");
            const string updateSql = @"
UPDATE delivery_requests
SET runner_id = @runnerId, status = 'accepted', updated_at = @updated_at
WHERE id = @requestId AND status = 'pending';";

            var parameters = new[]
            {
                AdoNetDbHelper.CreateParameter("@requestId", requestId),
                AdoNetDbHelper.CreateParameter("@runnerId", dto.RunnerId),
                AdoNetDbHelper.CreateParameter("@updated_at", now)
            };

            int affected = await _dbHelper.ExecuteNonQueryAsync(updateSql, parameters);
            if (affected == 0)
            {
                return Conflict(new { message = "Delivery request is no longer pending or does not exist." });
            }

            return Ok(new { message = "Delivery request successfully accepted by runner." });
        }

        /// <summary>
        /// Verifies delivery OTP, completes delivery, and credits runner rewards via atomic ADO.NET transaction.
        /// </summary>
        [HttpPost("complete/{requestId}")]
        public async Task<IActionResult> CompleteDelivery(string requestId, [FromBody] CompleteDeliveryDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Read request details
            const string selectSql = "SELECT runner_id, delivery_fee, delivery_otp, status FROM delivery_requests WHERE id = @id;";
            var req = await _dbHelper.ExecuteSingleAsync(selectSql, reader => new
            {
                RunnerId = reader.IsDBNull(0) ? null : reader.GetString(0),
                DeliveryFee = reader.GetDouble(1),
                DeliveryOtp = reader.IsDBNull(2) ? null : reader.GetString(2),
                Status = reader.GetString(3)
            }, AdoNetDbHelper.CreateParameter("@id", requestId));

            if (req == null)
            {
                return NotFound(new { message = "Delivery request not found." });
            }

            if (req.RunnerId != dto.RunnerId)
            {
                return Forbid("You are not the assigned runner for this delivery.");
            }

            if (req.DeliveryOtp != dto.DeliveryOtp.Trim())
            {
                return BadRequest(new { message = "Invalid OTP. Student must confirm delivery code." });
            }

            var now = DateTime.UtcNow.ToString("o");

            await _dbHelper.ExecuteTransactionAsync(async (conn, trans) =>
            {
                // 1. Mark request as delivered
                const string updateReqSql = @"
UPDATE delivery_requests
SET status = 'delivered', updated_at = @updated_at
WHERE id = @id;";

                await using (var cmd = new SqliteCommand(updateReqSql, conn, trans))
                {
                    cmd.Parameters.AddWithValue("@id", requestId);
                    cmd.Parameters.AddWithValue("@updated_at", now);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 2. Credit runner reward balance
                const string creditRunnerSql = @"
UPDATE users
SET reward_balance = reward_balance + @fee, updated_at = @updated_at
WHERE id = @runnerId;";

                await using (var cmd2 = new SqliteCommand(creditRunnerSql, conn, trans))
                {
                    cmd2.Parameters.AddWithValue("@runnerId", dto.RunnerId);
                    cmd2.Parameters.AddWithValue("@fee", req.DeliveryFee);
                    cmd2.Parameters.AddWithValue("@updated_at", now);
                    await cmd2.ExecuteNonQueryAsync();
                }
            });

            return Ok(new
            {
                message = "Delivery completed successfully! Reward credited to runner wallet.",
                rewardEarned = req.DeliveryFee
            });
        }
    }
}
