using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    private IUserRepository userRepository;
    private IMapper mapper;
    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();
        var userDto = mapper.Map<UserDto>(user);
        return Ok(userDto);
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] UserCreationDto user)
    {
        if (user == null)
            return BadRequest();
        if (ModelState.IsValid && !user.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError("Login", "Unaccepted symbols, only letter or digits are acceptable");
        if(!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var userEntity = mapper.Map<UserEntity>(user);
        var insetedUser = userRepository.Insert(userEntity);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = insetedUser.Id },
            insetedUser.Id);
    }
}