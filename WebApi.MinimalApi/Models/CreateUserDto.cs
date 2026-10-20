using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class CreateUserDto
{
    [Required]
    public string Login { get; set; } = string.Empty;

    public string FirstName { get; set; } = "John";

    public string LastName { get; set; } = "Doe";
}