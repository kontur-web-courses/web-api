using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class PutUserDto
{
    [Required]
    public String FirstName { get; set; }
    [Required]
    public String LastName { get; set; }
    
    [Required]
    [RegularExpression("^[0-9\\p{L}]*$", ErrorMessage = "Login should contain only letters or digits")]
    public String Login { get; set; }
    
}