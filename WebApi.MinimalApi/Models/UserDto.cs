using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class UserDto
{ 
    public Guid Id { get; set; }
    public string Login { get; set; }
    public string FullName { get; set; }
    public int GamesPlayed { get; set; }
    public Guid? CurrentGameId { get; set; }
}

public class UserCreationDto
{
    [Required]
    public string Login { get; set; }

    [DefaultValue("John")]
    public string FirstName { get; set; }

    [DefaultValue("Doe")]
    public string LastName { get; set; }
}