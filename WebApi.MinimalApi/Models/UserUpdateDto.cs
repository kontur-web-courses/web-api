using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class UserUpdateDto
{
    [Required]
    [RegularExpression("^[0-9\\p{L}]*$", ErrorMessage = "Use only letters or digits for login")]
    public string Login { get; set; }

    public string FirstName { get; set; }
    public string LastName { get; set; }
}