using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

public class UserDto
{
    [Required]
    public string Login { get; set; }
    [DefaultValue("John")]
    public string FirstName { get; set; }
    [DefaultValue("Doe")]
    public string LastName { get; set; }
}

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

    [Produces("application/json", "application/xml")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    public ActionResult<Models.UserDto> GetUserById([FromRoute] System.Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
        {
            return NotFound();
        }
        var userDto = mapper.Map<Models.UserDto>(user);
        return Ok(userDto);
    }
    
    [Produces("application/json", "application/xml")]
    [HttpPost]
    public IActionResult CreateUser([FromBody] UserDto user)
    {
        var createdUserEntity = mapper.Map<UserEntity>(user);
        if (user == null)
        {
            return BadRequest();
        }
        
        if (user.Login == null || !user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError(nameof(user.Login), "Сообщение об ошибке");
            return UnprocessableEntity(ModelState);
        }
        var insertedUser = userRepository.Insert(createdUserEntity);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = insertedUser.Id },
            insertedUser.Id);
    }
    
    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] System.Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
        {
            return NotFound();
        } 
        userRepository.Delete(userId);
        return NoContent();
    }
}