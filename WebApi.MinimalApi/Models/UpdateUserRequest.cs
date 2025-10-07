using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class UpdateUserRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "First name cannot be empty")]
    [RegularExpression("^[0-9\\p{L}]*$", ErrorMessage = "Login should contain only letters or digits")]
    public string? login { get; set; }
    [Required]
    [MinLength(1, ErrorMessage = "First name cannot be empty")]
    public string? firstName { get; set; }
    [Required]
    [MinLength(1, ErrorMessage = "First name cannot be empty")]
    public string? lastName { get; set; }
}