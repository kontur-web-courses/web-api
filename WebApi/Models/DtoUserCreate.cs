using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApi.Models
{
    public class DtoUserCreate
    {
        [Required]
        public string Login { get; set; }
        [DefaultValue("Doe")]
        public string LastName { get; set; }
        [DefaultValue("John")]
        public string FirstName { get; set; }
    }
}