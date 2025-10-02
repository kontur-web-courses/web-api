using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json", "application/xml")]
public class UsersController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    
    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    [HttpHead("{userId}")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user != null)
        {
            var userDto = _mapper.Map<UserDto>(user);
            return HttpMethods.IsHead(Request.Method)
                ? new ContentResult { StatusCode = 200, ContentType = "application/json; charset=utf-8"}
                : Ok(userDto);
        }
        
        return NotFound();
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] UserForCreateDto? user)
    {
        if (user == null)
        {
            return BadRequest();
        }
        
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        if (!user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("Login", "Login should contain only letters or digits");
            return UnprocessableEntity(ModelState);
        }
        
        var userEntity = _mapper.Map<UserEntity>(user);
        var createdUserEntity = _userRepository.Insert(userEntity);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }

    [HttpPut("{userId}")]
    public IActionResult PutUser([FromRoute] Guid userId, [FromBody] UserForPutDto? user)
    {
        if (user == null || userId == Guid.Empty)
        {
            return BadRequest();
        }
        
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = new UserEntity(userId);
        _mapper.Map(user, userEntity);
        _userRepository.UpdateOrInsert(userEntity, out var isInserted);

        if (isInserted)
        {
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id);
        }
        
        return NoContent();
    }

    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser(
        [FromRoute] Guid userId,
        [FromBody] JsonPatchDocument<UserToUpdateDto>? patchDoc)
    {
        if (userId == Guid.Empty)
        {
            return NotFound();
        }
        
        var user = _userRepository.FindById(userId);
        
        if (patchDoc == null)
            return BadRequest();
        
        if (user == null) 
            return NotFound();
        
        var userDto = new UserToUpdateDto();
        _mapper.Map(user, userDto);
        patchDoc.ApplyTo(userDto, ModelState);
        if (!ModelState.IsValid || !TryValidateModel(userDto))
            return UnprocessableEntity(ModelState);
        
        
        var userEntity = new UserEntity(userId);
        _mapper.Map(user, userEntity);
        _userRepository.Update(userEntity);

        return NoContent();
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (userId == Guid.Empty || _userRepository.FindById(userId) == null)
        {
            return NotFound();
        }

        _userRepository.Delete(userId);

        return NoContent();
    }
}