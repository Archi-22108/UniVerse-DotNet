using System;

namespace UniVerse.Server.Models
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? EnrollmentNumber { get; set; }
        public string Role { get; set; } = "student"; // "student", "runner", "admin"
        public string? HostelName { get; set; }
        public string? RoomNumber { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsActiveRunner { get; set; }
        public double RewardBalance { get; set; }
        public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");
        public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");
    }

    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? EnrollmentNumber { get; set; }
        public string Role { get; set; } = "student";
        public string? HostelName { get; set; }
        public string? RoomNumber { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsActiveRunner { get; set; }
        public double RewardBalance { get; set; }
        public string CreatedAt { get; set; } = string.Empty;

        public static UserDto FromEntity(User user) => new()
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            EnrollmentNumber = user.EnrollmentNumber,
            Role = user.Role,
            HostelName = user.HostelName,
            RoomNumber = user.RoomNumber,
            PhoneNumber = user.PhoneNumber,
            IsActiveRunner = user.IsActiveRunner,
            RewardBalance = user.RewardBalance,
            CreatedAt = user.CreatedAt
        };
    }
}
