using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class UserCreateDto
{
    [DefaultValue("John")]
    public string FirstName { get; set; }
    [DefaultValue("Doe")]
    public string LastName { get; set; }
    [Required]
    public string Login { get; set; }
}