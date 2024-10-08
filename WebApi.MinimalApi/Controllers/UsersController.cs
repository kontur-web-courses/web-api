using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private readonly IMapper mapper;
    private readonly IUserRepository userRepository;

    public UsersController(IUserRepository userRepository,
                           IMapper mapper)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
    }
    
    [HttpHead("{userId}")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();
        if (HttpMethods.IsHead(HttpContext.Request.Method))
            Response.Body = Stream.Null;
        var userDto = mapper.Map<UserDto>(user);
        return Ok(userDto);
    }

    [HttpPost("")]
    public IActionResult CreateUser([FromBody] UserToCreateDto? user)
    {
        if (user == null)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var userEntity = mapper.Map<UserEntity>(user);
        userEntity = userRepository.Insert(userEntity);

        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = userEntity.Id },
            userEntity.Id);
    }

    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromRoute] Guid? userId, [FromBody] UserToUpdateDto? user)
    {
        if (user == null || ModelState.IsInvalid(nameof(userId)))
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        userId ??= Guid.NewGuid();
        var mappedUser = mapper.Map(user, new UserEntity(userId.Value));
        userRepository.UpdateOrInsert(mappedUser, out var inserted);
        return inserted
            ? CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userId.Value },
                mappedUser.Id)
            : NoContent();
    }

    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserToUpdateDto>? patchDocument)
    {
        if (patchDocument == null)
            return BadRequest();
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        var userToUpdate = mapper.Map<UserToUpdateDto>(user);
        patchDocument.ApplyTo(userToUpdate, ModelState);
        if (!TryValidateModel(userToUpdate))
            return UnprocessableEntity(ModelState);
        mapper.Map(userToUpdate, user);
        userRepository.Update(user);
        return NoContent();
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (!ModelState.IsValid)
            return NotFound();
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();
        userRepository.Delete(userId);
        return NoContent();
    }
}