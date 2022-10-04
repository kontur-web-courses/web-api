using System;
using System.ComponentModel.DataAnnotations;

namespace WebApi.Models
{
    public class UserToCreateDto
    {
        [Required]
        public string Login { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}