using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models
{
    public class guid
    {
        [Required]
        public string Login { get; set; }
        //[DefaultValue("")]
        public string FirstName { get; set; }
        //[DefaultValue("")]
        public string LastName { get; set; }
    }
}
