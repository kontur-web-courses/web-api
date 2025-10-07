using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class PersonDTO
{ 
    public Guid Id { get; set; }
    
    [Required]
    public string Login { get; set; }
    
    [DefaultValue("John")]
    public string FirstName { get; set; }
    
    [DefaultValue("Doe")]
    public string LastName { get; set; }
}