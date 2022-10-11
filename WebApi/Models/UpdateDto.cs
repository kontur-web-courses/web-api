namespace WebApi.Models
{
    using System.ComponentModel.DataAnnotations;

    public record UpdateDto(
        [Required]
        [RegularExpression("^[0-9\\p{L}]*$", ErrorMessage = "Login should contain only letters or digits")]
        string Login,
        [Required] string FirstName,
        [Required] string LastName);
}