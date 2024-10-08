using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class CreateUserRequest
{
    [Required]
    public string Login { get; set; } = default!;
    
    [DefaultValue("John")]
    public string FirstName { get; set; } = default!;
    
    [DefaultValue("Doe")]
    public string LastName { get; set; } = default!;
}