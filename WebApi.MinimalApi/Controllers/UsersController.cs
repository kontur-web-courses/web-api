using System.Security.Cryptography.X509Certificates;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    private IUserRepository Repository;
    private IMapper Mapper;
    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        Repository = userRepository;
        Mapper = mapper;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = Repository.FindById(userId);
        if (user is null)
            return NotFound();
        var result = Mapper.Map<UserDto>(user);
        return Ok(result);
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] AddUserDto user)
    {
        var createdUserEntity = Mapper.Map<UserEntity>(user);
        if (createdUserEntity is null)
            return BadRequest();
        if(createdUserEntity.Login?.All(char.IsLetterOrDigit) == false)
            ModelState.AddModelError(nameof(createdUserEntity.Login).ToLower(), "Login must be letters or digits");
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        Repository.Insert(createdUserEntity);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromQuery] Guid userId, [FromBody] UpdateUserDto user)
    {
        if (user == null)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        if (userId == Guid.Empty)
            return BadRequest();

        var userEntity = Mapper.Map(user, new UserEntity(userId));

        Repository.UpdateOrInsert(userEntity, out var isInsert);

        if (isInsert)
            return CreatedAtRoute(
                nameof(GetUserById),
                new {userId = userId},
                userId);
        return NoContent();
    }
}