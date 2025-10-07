using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class CreateUserRequest
{
    [Required] 
    public required string Login { get; set; }
    
    [DefaultValue("John")]
    public string? FirstName { get; set; }
    
    [DefaultValue("Doe")]
    public string? LastName { get; set; }
}