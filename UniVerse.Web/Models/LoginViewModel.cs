using System.ComponentModel.DataAnnotations;

namespace UniVerse.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Student email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = true;

    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }
}
