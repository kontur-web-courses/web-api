using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public record UpdateUserDto
(
    [Required]
    [RegularExpression("^[0-9\\p{L}]*$", ErrorMessage = "Login should contain only letters or digits")]
    string Login,
    [Required]
    string FirstName,
    [Required]
    string LastName
);