using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models
{
    public class guid
    {
        [Required]
        public string Login { get; set; }
        [DefaultValue("-_-")]
        public string FirstName { get; set; }
        [DefaultValue("-_-")]
        public string LastName { get; set; }
    }
}
