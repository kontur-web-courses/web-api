using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация

    private IUserRepository userRepository;
    public UsersController(IUserRepository userRepository)
    {
        this.userRepository = userRepository;
    }

    [Produces("application/json", "application/xml")]
    [HttpGet("{userId}")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
        {
            return NotFound();
        }
        var dto = new UserDto();
        dto.Id = user.Id;
        dto.Login = user.Login;
        dto.GamesPlayed = user.GamesPlayed;
        dto.CurrentGameId = user.CurrentGameId;
        dto.FullName = $"{user.LastName} {user.FirstName}";
        return Ok(dto);
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] object user)
    {
        throw new NotImplementedException();
    }
}