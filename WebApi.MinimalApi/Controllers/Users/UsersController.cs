using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers.Users;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json", "application/xml")]
public class UsersController : Controller
{
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;

    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        this.userRepository = userRepository ??
                              throw new ArgumentException("Null reference", nameof(userRepository));
        this.mapper = mapper;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        return user is null ? NotFound() : Ok(mapper.Map<UserDto>(user));
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] CreateUserRequest? user)
    {
        if (user is null)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        if (!user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError(nameof(user.Login), "Login has invalid chars");
            return UnprocessableEntity(ModelState);
        }

        var entity = userRepository.Insert(mapper.Map<UserEntity>(user));


        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = entity.Id },
            entity.Id);
    }

    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromBody] UpdateUserRequest? user, [FromRoute] Guid userId)
    {
        if (user is null || userId == Guid.Empty)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var entity = mapper.Map(new UserEntity(userId), mapper.Map<UserEntity>(user));
        userRepository.UpdateOrInsert(entity, out var isInserted);
        if (isInserted)
        {
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = entity.Id },
                entity.Id);
        }
        return NoContent();
    }

    [HttpPatch("{userId}")]
    public IActionResult PatchUser([FromBody] JsonPatchDocument<PatchUserRequest>? patchDoc, [FromRoute] Guid userId)
    {
        if (patchDoc is null)
        {
            return BadRequest();
        }

        if (userId == Guid.Empty)
        {
            return NotFound();
        }
        
        var patch = new PatchUserRequest();
        patchDoc.ApplyTo(patch);
        
        if (!TryValidateModel(patch) || !ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var userEntity = mapper.Map(new UserEntity(userId), mapper.Map<UserEntity>(patch));
        userRepository.Update(userEntity);
        if (userRepository.FindById(userId) is null)
        {
            return NotFound();
        }

        return NoContent();
    }
}