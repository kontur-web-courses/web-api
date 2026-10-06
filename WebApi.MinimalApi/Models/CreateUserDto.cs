using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class CreateUserDto
{
    [Required]
    [RegularExpression(@"^[a-zA-Z0-9].*")]
    public string Login { get; set; }
    [DefaultValue("John")]
    public string FirstName { get; set; }
    [DefaultValue("Doe")]
    public string LastName { get; set; }
}