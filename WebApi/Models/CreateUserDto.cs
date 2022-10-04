namespace WebApi.Models
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    public record CreateUserDto(
        [Required]
        [RegularExpression("^[0-9\\p{L}]*$", ErrorMessage = "Login should contain only letters or digits")]
        string Login,
        [DefaultValue("John")] string FirstName,
        [DefaultValue("Doe")] string LastName);
}