using System;

namespace UniVerse.Web.Models;

/// <summary>
/// Domain model for Campus Students & Runners.
/// Demonstrates C# OOP inheritance from BaseEntity.
/// </summary>
public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Student"; // Student or Runner
    public string HostelBlock { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string? Department { get; set; }
    public string? Semester { get; set; }
}
