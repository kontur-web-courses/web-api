using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class AddUserDto
{
    [Required]
    public string Login { get; set; }

    [DefaultValue("John")]
    public string FirstName { get; set; } = "John";

    [DefaultValue("Doe")]
    public string LastName { get; set; } = "Doe";
}