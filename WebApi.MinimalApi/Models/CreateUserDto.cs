using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class CreateUserDto
{
    [Required]
    public string Login { get; set; }
    
    [DefaultValue("a")]
    public string FirstName { get; set; }
    
    [DefaultValue("b")]
    public string LastName { get; set; }
}