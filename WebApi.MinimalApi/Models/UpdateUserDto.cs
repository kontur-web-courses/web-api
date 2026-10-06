using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class UpdateUserDto
{
    [Required]
    [RegularExpression(@"^[a-zA-Z0-9].*")]
    public string Login { get; set; }
    [Required]
    public string FirstName { get; set; }
    [Required]
    public string LastName { get; set; }
}