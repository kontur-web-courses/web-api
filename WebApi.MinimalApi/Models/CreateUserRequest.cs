using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class CreateUserRequest
{
    [Required]
    public required string Login { get; init; }
    [DefaultValue("John")]
    public string FirstName { get; init; }
    [DefaultValue("Doe")]
    public string LastName { get; init; }
}