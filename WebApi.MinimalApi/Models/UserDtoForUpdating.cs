using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class UserDtoForUpdating
{
    [Required]
    [RegularExpression("^[0-9\\p{L}]*$", ErrorMessage = "Login should contain only letters or digits")]
    public string Login { get; set; }

    [Required]
    public string FirstName { get; set; }

    [Required]
    public string LastName { get; set; }

    // Остальные поля, которые может потребоваться при маппинге (необязательно для валидации)
    public int GamesPlayed { get; set; }
    public Guid? CurrentGameId { get; set; }
}