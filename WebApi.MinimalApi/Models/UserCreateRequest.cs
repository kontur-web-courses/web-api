using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public record UserCreateRequest(
    [RegularExpression("^[0-9\\p{L}]*$", ErrorMessage = "Login should contain only letters or digits")]
    [Required] string Login,
    [DefaultValue("John")] string FirstName,
    [DefaultValue("Doe")] string LastName);