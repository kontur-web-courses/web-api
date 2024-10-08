using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[Produces("application/json", "application/xml")]
[ApiController]
public class UsersController : Controller
{
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;
    
    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        
        if (user == null)
            return NotFound();
        
        return Ok(mapper.Map<UserDto>(user));
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] CreateUserDto createUser)
    {
        if (createUser == null)
            return BadRequest();
        
        var createdUserEntity = mapper.Map<UserEntity>(createUser);
        
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        if (!createdUserEntity.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError("Login", "Login must only contain letters or digits");
        
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        createdUserEntity = userRepository.Insert(createdUserEntity);
        
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }
}