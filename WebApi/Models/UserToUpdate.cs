using System.ComponentModel.DataAnnotations;

namespace WebApi.Models
{
    public class UserToUpdate
    {
        [Required]
        [RegularExpression("^[0-9\\p{L}]*$", ErrorMessage = "Login should contain only letters or digits")]
        public string Login { get; set; }

        [Required]
        public string LastName { get; set; }
        
        [Required]
        public string FirstName { get; set; }
    }
}