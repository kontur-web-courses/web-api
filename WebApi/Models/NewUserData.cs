using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApi.Models
{
    public class NewUserData
    {
        [Required]
        [RegularExpression("^[0-9\\p{L}]*$", ErrorMessage = "Login should contain only letters or digits")]
        public string Login { get; set; }

        [DefaultValue("John")]
        public string FirstName { get; set; }

        [DefaultValue("Doe")]
        public string LastName { get; set; }
    }
}