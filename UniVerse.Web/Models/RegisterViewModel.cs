using System.ComponentModel.DataAnnotations;

namespace UniVerse.Web.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    [MinLength(2, ErrorMessage = "Name must be at least 2 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "University email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@marwadiuniversity\.ac\.in$", ErrorMessage = "Must be a @marwadiuniversity.ac.in email address.")]
    [Display(Name = "University Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the Terms & Privacy Policy to continue.")]
    [Display(Name = "Terms & Privacy")]
    public bool AgreeTerms { get; set; } = false;

    public string? ErrorMessage { get; set; }
}
