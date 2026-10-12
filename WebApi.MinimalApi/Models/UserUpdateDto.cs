using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class UserUpdateDto
{
    [Required]
    [RegularExpression("^[0-9\\p{L}]*$", ErrorMessage = "invalid login")]
    public string Login { get; set; }
    [Required]
    public string FirstName { get; set; }
    [Required]
    public string LastName { get; set; }
}