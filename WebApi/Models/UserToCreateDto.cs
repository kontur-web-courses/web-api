using System;

namespace WebApi.Models
{
    public class UserToCreateDto
    {
        public string Login { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}