namespace WebApi.MinimalApi.Models;

public class CreateUserDto
{
    
    [DefaultValue("John")]
    public string FirstName { get; set; }
    
    [DefaultValue("Doe")]
    public string LastName { get; set; }
    
    
    [Required]
    [RegularExpression("^[0-9\\p{L}]*$", ErrorMessage = "Login should contain only letters or digits")]
    public string Login { get; set; }
}