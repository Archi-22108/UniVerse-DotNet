using System;

namespace UniVerse.Server.Models
{
    public class Notification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "system"; // system, delivery, request, runner, status_broadcasted, status_in_transit, status_delivered, status_accepted, status_cancelled
        public string? ReferenceId { get; set; }
        public bool IsRead { get; set; } = false;
        public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");
    }
}
