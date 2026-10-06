using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    private readonly IUserRepository repository;
    private readonly IMapper mapper;
    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        repository = userRepository;
        this.mapper = mapper;
    }

    [HttpGet("{userId}")]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var userEntity = repository.FindById(userId);
        if (userEntity == null)
            return NotFound();
        var userDto = mapper.Map<UserDto>(userEntity);
        return Ok(userDto);
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] object user)
    {
        throw new NotImplementedException();
    }
}