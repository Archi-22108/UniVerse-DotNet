using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace UniVerse.Web.Models;

/// <summary>
/// Strongly-typed ViewModel for Student Profile Management.
/// Demonstrates C# OOP Encapsulation and Data Annotations.
/// </summary>
public class ProfileViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "archi.kumari126697@marwadiuniversity.ac.in";

    [Required]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = "3166_ARCHI KUMARI";

    public string? ProfilePictureUrl { get; set; }

    public string? Department { get; set; }

    public string? Semester { get; set; }

    public string Initial => !string.IsNullOrWhiteSpace(FullName) ? FullName.Trim().Substring(0, 1) : "3";

    public string RatingText { get; set; } = "No ratings yet";

    public bool RemovePhoto { get; set; } = false;

    public IFormFile? PhotoFile { get; set; }
}
