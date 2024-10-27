using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models.Requests;

public class UpdateUserRequest
{
    [Required]
    [RegularExpression("[a-zA-Z0-9]+", ErrorMessage="Login should contain only letters or digits")]
    public string Login { get; set; }
    [Required]
    public string FirstName { get; set; }
    [Required]
    public string LastName { get; set; }
}