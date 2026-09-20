using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UniVerse.Server.Data;
using UniVerse.Server.Data.Repositories;
using UniVerse.Server.Models;

namespace UniVerse.Server.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthApiController : ControllerBase
    {
        private readonly IUserRepository _userRepo;
        private readonly ILogger<AuthApiController> _logger;

        public AuthApiController(IUserRepository userRepo, ILogger<AuthApiController> logger)
        {
            _userRepo = userRepo;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto { Success = false, Message = "Invalid registration payload." });
            }

            var cleanEmail = request.Email.Trim().ToLowerInvariant();
            var existing = await _userRepo.GetByEmailAsync(cleanEmail);
            if (existing != null)
            {
                return Conflict(new AuthResponseDto { Success = false, Message = "A user with this email already exists." });
            }

            var newId = Guid.NewGuid().ToString();
            var passwordHash = DbInitializer.HashPassword(request.Password);
            var now = DateTime.UtcNow.ToString("o");

            var user = new User
            {
                Id = newId,
                Email = cleanEmail,
                PasswordHash = passwordHash,
                FullName = request.FullName.Trim(),
                EnrollmentNumber = request.EnrollmentNumber?.Trim(),
                Role = request.Role.ToLowerInvariant(),
                HostelName = request.HostelName?.Trim(),
                RoomNumber = request.RoomNumber?.Trim(),
                PhoneNumber = request.PhoneNumber?.Trim(),
                IsActiveRunner = request.Role.Equals("runner", StringComparison.OrdinalIgnoreCase),
                RewardBalance = 0.0,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _userRepo.CreateUserAsync(user);

            return Ok(new AuthResponseDto
            {
                Success = true,
                Message = "Registration successful via ADO.NET.",
                Token = $"mock-jwt-token-{newId}",
                User = UserDto.FromEntity(user)
            });
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto { Success = false, Message = "Invalid login payload." });
            }

            var user = await _userRepo.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return Unauthorized(new AuthResponseDto { Success = false, Message = "Invalid email or password." });
            }

            var calculatedHash = DbInitializer.HashPassword(request.Password);
            if (user.PasswordHash != calculatedHash)
            {
                return Unauthorized(new AuthResponseDto { Success = false, Message = "Invalid email or password." });
            }

            return Ok(new AuthResponseDto
            {
                Success = true,
                Message = "Login successful via ADO.NET.",
                Token = $"mock-jwt-token-{user.Id}",
                User = UserDto.FromEntity(user)
            });
        }

        [HttpGet("users")]
        public async Task<ActionResult<List<UserDto>>> GetAllUsers()
        {
            var users = await _userRepo.GetAllUsersAsync();
            var dtos = new List<UserDto>();
            foreach (var u in users)
            {
                dtos.Add(UserDto.FromEntity(u));
            }
            return Ok(dtos);
        }
    }
}
