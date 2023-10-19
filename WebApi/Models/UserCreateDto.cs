using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace WebApi.Models
{
    public class UserCreateDto
    {
        [Required]
        [RegularExpression(@"[A-Za-z0-9]+")]
        public string Login { get; set; }
        [DefaultValue("John")]
        public string FirstName { get; set; }
        [DefaultValue("Doe")]
        public string LastName { get; set; }
    }
}