namespace WebApi.MinimalApi.Models;
using WebApi.MinimalApi.Domain;
public class UserDto
{ 
    public Guid Id { get; set; }
    public string Login { get; set; }
    public string FullName { get; set; }
    public int GamesPlayed { get; set; }
    public Guid? CurrentGameId { get; set; }
}