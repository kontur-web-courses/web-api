using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class PartiallyUpdateUserRequest
{
    [Required]
    [RegularExpression("^[0-9\\p{L}]*$", ErrorMessage = "Login should contain only letters or digits")]
    public required string Login { get; set; }
    
    [Required]
    public required string FirstName { get; set; }
    
    [Required]
    public required string LastName { get; set; }
}