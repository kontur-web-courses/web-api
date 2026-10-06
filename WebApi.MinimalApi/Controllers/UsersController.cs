using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    IUserRepository userRepository;
    IMapper mapper;
    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        this.userRepository = userRepository;
        this.mapper  = mapper;
    }

    [HttpGet("{userId}")]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();
        return Ok(mapper.Map<UserDto>(user));
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] object user)
    {
        throw new NotImplementedException();
    }

    private UserDto СonvertUserEntityToDto(UserEntity user)
    {
        var userDto = new UserDto();
        userDto.Id = user.Id;
        userDto.Login = user.Login;
        userDto.FullName = $"{user.FirstName} {user.LastName}";
        userDto.GamesPlayed = user.GamesPlayed;
        userDto.CurrentGameId = user.CurrentGameId;
        return userDto;
    }
}