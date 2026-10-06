using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class UpdateUserDto
{
    [Required]
    [RegularExpression("^[0-9\\p{L}]*$", ErrorMessage = "Login should contain only letters or digits")]
    public string Login { get; init; }
    
    [Required]
    public string LastName { get; init; }
    
    [Required]
    public string FirstName { get; init; }
}