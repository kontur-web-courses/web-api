using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class CreateUserDto
{
    [Required]
    public string? Login { get; set; }
    
    [Required]
    [DefaultValue("John")]   
    public string FirstName { get; set; }
 
    [Required]
    [DefaultValue("Doe")]   
    public string LastName { get; set; }
}