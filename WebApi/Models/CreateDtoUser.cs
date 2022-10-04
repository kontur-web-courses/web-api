using System;

namespace WebApi.Models
{
    public class CreateDtoUser
    {
        public string Login { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
    }
}