using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/users")]
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
        if (user is null)
            return NotFound();
        var result = mapper.Map<UserDto>(user);
        return result;
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] PostUserDto user)
    {
        var createdUserEntity = mapper.Map<UserEntity>(user);
        if (createdUserEntity is null)
            return BadRequest();
        if(createdUserEntity.Login?.All(char.IsLetterOrDigit) == false)
            ModelState.AddModelError(nameof(createdUserEntity.Login).ToLower(), "login is letters and digits");
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        var userInsert = userRepository.Insert(createdUserEntity);
        
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = userInsert.Id },
            userInsert.Id);
    }

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser(Guid userId, [FromBody] PutUserDto userData)
    {
        if (userId == Guid.Empty || userData is null)
            return BadRequest();
        var putData = mapper.Map(userData, new UserEntity(userId));
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        userRepository.UpdateOrInsert(putData, out var isInserted);
        if (isInserted)
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userId },
                userId);
        return NoContent();
    }

    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser(Guid userId, [FromBody] JsonPatchDocument<PutUserDto> patchDoc)
    {
        if (patchDoc is null)
            return BadRequest();

        var user = userRepository.FindById(userId);
        
        var updateUserDto = mapper.Map(user, new PutUserDto());
        patchDoc.ApplyTo(updateUserDto, ModelState);
        TryValidateModel(updateUserDto);
        if(!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        mapper.Map(updateUserDto, user);
        userRepository.Update(user);
        return NoContent();
    }
    
    [HttpDelete("{userId}")]
    public IActionResult DeleteUser(Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
            return NotFound();
        userRepository.Delete(userId);
        return NoContent();
    }
    
    
    
    
    
    
    [HttpOptions(Name = nameof(GetUsersOptions))]
    public IActionResult GetUsersOptions()
    {
        Response.Headers.Add("Allow", "POST, GET, OPTIONS");

        return Ok();
    }
}