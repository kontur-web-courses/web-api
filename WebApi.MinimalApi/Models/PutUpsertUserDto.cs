using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class PutUpsertUserDto
{
    [Required]
    [RegularExpression("^[0-9\\p{L}]*$", ErrorMessage = "Login should contain only letters or digits")]
    public string Login { get; set; }

    public string FirstName { get; set; }
    public string LastName { get; set; }
}