using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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

    private IUserRepository userRepository;
    private IMapper mapper;
    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
    }

    [Produces("application/json", "application/xml")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [HttpHead("{userId}")]
    public ActionResult<Models.UserDto> GetUserById([FromRoute] System.Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
        {
            return NotFound();
        }
        if (HttpContext.Request.Method == HttpMethods.Head)
        {
            Response.Headers["Content-Type"] = "application/json; charset=utf-8";
            return Ok();
        }
        var userDto = mapper.Map<Models.UserDto>(user);
        return Ok(userDto);
    }
    
    [Produces("application/json", "application/xml")]
    [HttpPost]
    public IActionResult CreateUser([FromBody] CreateUserDto createUser)
    {
        var createdUserEntity = mapper.Map<UserEntity>(createUser);
        if (createUser == null)
        {
            return BadRequest();
        }
        
        if (createUser.Login == null || !createUser.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError(nameof(createUser.Login), "Сообщение об ошибке");
            return UnprocessableEntity(ModelState);
        }
        var insertedUser = userRepository.Insert(createdUserEntity);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = insertedUser.Id },
            insertedUser.Id);
    }
    
    [Produces("application/json", "application/xml")]
    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromBody] UpdateUserDto createUser , [FromRoute] Guid userId)
    {
        
        var createdUserEntity = mapper.Map(new UserEntity(userId), mapper.Map<UserEntity>(createUser));
        if (createUser is null || userId == Guid.Empty)
        {
            return BadRequest();
        }
        
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }
        
        userRepository.UpdateOrInsert(createdUserEntity, out bool isInsert);
        
        if (isInsert)
            return CreatedAtAction(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);

        return NoContent();
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