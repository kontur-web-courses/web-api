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
    public IActionResult CreateUser([FromBody] CreatedUserDto createdUser)
    {
        if (createdUser == null)
            return BadRequest();
        
        var createdUserEntity = mapper.Map<UserEntity>(createdUser);
        
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        if (!createdUserEntity.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError("Login", "Login should contain only letters or digits");
        
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        createdUserEntity = userRepository.Insert(createdUserEntity);
        
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }

    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdatedUserDto updatedUser)
    {
        if (updatedUser == null || userId == Guid.Empty)
            return BadRequest();
        
        updatedUser.Id = userId;
        var updatedUserEntity = mapper.Map<UserEntity>(updatedUser);
        
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        userRepository.UpdateOrInsert(updatedUserEntity, out var isInserted);
        
        if (isInserted)
            return Created("User not found in repository. Created a new one successfully", updatedUserEntity);
        
        return NoContent();
    }
}