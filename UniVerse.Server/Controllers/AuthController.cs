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
    public class AuthController : ControllerBase
    {
        private readonly AdoNetDbHelper _dbHelper;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AdoNetDbHelper dbHelper, ILogger<AuthController> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new campus user using raw ADO.NET.
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto { Success = false, Message = "Invalid registration payload." });
            }

            var cleanEmail = request.Email.Trim().ToLowerInvariant();

            // 1. ADO.NET Check: Does user email already exist?
            const string checkSql = "SELECT COUNT(*) FROM users WHERE email = @email;";
            var paramEmail = AdoNetDbHelper.CreateParameter("@email", cleanEmail);
            long existingCount = await _dbHelper.ExecuteScalarAsync<long>(checkSql, paramEmail);

            if (existingCount > 0)
            {
                return Conflict(new AuthResponseDto { Success = false, Message = "A user with this email already exists." });
            }

            // 2. Hash password & prepare entity
            var newId = Guid.NewGuid().ToString();
            var passwordHash = DbInitializer.HashPassword(request.Password);
            var now = DateTime.UtcNow.ToString("o");

            // 3. ADO.NET Insert
            const string insertSql = @"
INSERT INTO users (id, email, password_hash, full_name, enrollment_number, role, hostel_name, room_number, phone_number, is_active_runner, reward_balance, created_at, updated_at)
VALUES (@id, @email, @password_hash, @full_name, @enrollment_number, @role, @hostel_name, @room_number, @phone_number, @is_active_runner, @reward_balance, @created_at, @updated_at);";

            var parameters = new[]
            {
                AdoNetDbHelper.CreateParameter("@id", newId),
                AdoNetDbHelper.CreateParameter("@email", cleanEmail),
                AdoNetDbHelper.CreateParameter("@password_hash", passwordHash),
                AdoNetDbHelper.CreateParameter("@full_name", request.FullName.Trim()),
                AdoNetDbHelper.CreateParameter("@enrollment_number", request.EnrollmentNumber?.Trim()),
                AdoNetDbHelper.CreateParameter("@role", request.Role.ToLowerInvariant()),
                AdoNetDbHelper.CreateParameter("@hostel_name", request.HostelName?.Trim()),
                AdoNetDbHelper.CreateParameter("@room_number", request.RoomNumber?.Trim()),
                AdoNetDbHelper.CreateParameter("@phone_number", request.PhoneNumber?.Trim()),
                AdoNetDbHelper.CreateParameter("@is_active_runner", request.Role.Equals("runner", StringComparison.OrdinalIgnoreCase) ? 1 : 0),
                AdoNetDbHelper.CreateParameter("@reward_balance", 0.0),
                AdoNetDbHelper.CreateParameter("@created_at", now),
                AdoNetDbHelper.CreateParameter("@updated_at", now)
            };

            await _dbHelper.ExecuteNonQueryAsync(insertSql, parameters);

            var userDto = new UserDto
            {
                Id = newId,
                Email = cleanEmail,
                FullName = request.FullName.Trim(),
                EnrollmentNumber = request.EnrollmentNumber?.Trim(),
                Role = request.Role.ToLowerInvariant(),
                HostelName = request.HostelName?.Trim(),
                RoomNumber = request.RoomNumber?.Trim(),
                PhoneNumber = request.PhoneNumber?.Trim(),
                IsActiveRunner = request.Role.Equals("runner", StringComparison.OrdinalIgnoreCase),
                RewardBalance = 0.0,
                CreatedAt = now
            };

            return Ok(new AuthResponseDto
            {
                Success = true,
                Message = "Registration successful.",
                Token = $"mock-jwt-token-{newId}",
                User = userDto
            });
        }

        /// <summary>
        /// Authenticates a campus user using raw ADO.NET and SqliteDataReader.
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto { Success = false, Message = "Invalid login payload." });
            }

            var cleanEmail = request.Email.Trim().ToLowerInvariant();

            // ADO.NET Query with SqliteDataReader mapping
            const string querySql = @"
SELECT id, email, password_hash, full_name, enrollment_number, role, hostel_name, room_number, phone_number, is_active_runner, reward_balance, created_at, updated_at
FROM users
WHERE email = @email;";

            var paramEmail = AdoNetDbHelper.CreateParameter("@email", cleanEmail);

            var user = await _dbHelper.ExecuteSingleAsync<User>(querySql, reader => new User
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
            }, paramEmail);

            if (user == null)
            {
                return Unauthorized(new AuthResponseDto { Success = false, Message = "Invalid email or password." });
            }

            // Verify password
            var calculatedHash = DbInitializer.HashPassword(request.Password);
            if (user.PasswordHash != calculatedHash)
            {
                return Unauthorized(new AuthResponseDto { Success = false, Message = "Invalid email or password." });
            }

            return Ok(new AuthResponseDto
            {
                Success = true,
                Message = "Login successful.",
                Token = $"mock-jwt-token-{user.Id}",
                User = UserDto.FromEntity(user)
            });
        }

        /// <summary>
        /// Retrieves the current authenticated user's profile by ID.
        /// </summary>
        [HttpGet("me/{userId}")]
        public async Task<ActionResult<UserDto>> GetProfile(string userId)
        {
            const string querySql = @"
SELECT id, email, full_name, enrollment_number, role, hostel_name, room_number, phone_number, is_active_runner, reward_balance, created_at
FROM users
WHERE id = @id;";

            var paramId = AdoNetDbHelper.CreateParameter("@id", userId);

            var userDto = await _dbHelper.ExecuteSingleAsync<UserDto>(querySql, reader => new UserDto
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
            }, paramId);

            if (userDto == null)
            {
                return NotFound(new { message = "User not found." });
            }

            return Ok(userDto);
        }

        /// <summary>
        /// Lists all campus users (demonstrates ADO.NET ExecuteReaderAsync).
        /// </summary>
        [HttpGet("users")]
        public async Task<ActionResult<List<UserDto>>> GetAllUsers()
        {
            const string querySql = @"
SELECT id, email, full_name, enrollment_number, role, hostel_name, room_number, phone_number, is_active_runner, reward_balance, created_at
FROM users
ORDER BY created_at ASC;";

            var users = await _dbHelper.ExecuteReaderAsync<UserDto>(querySql, reader => new UserDto
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

            return Ok(users);
        }
    }
}
