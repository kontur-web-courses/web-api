using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    IUserRepository userRepository;
    IMapper mapper;
    
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
        if (user == null)
            return NotFound();
        return Ok(mapper.Map<UserDto>(user));
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] NewUserDto? newUserDto)
    {
        if (newUserDto == null)
            return BadRequest();
        
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        if (!CheckKeyIsValid(newUserDto.Login))
        {
            ModelState.AddModelError(nameof(NewUserDto.Login), "Логин должен состоять из цифр и букв");
            return UnprocessableEntity(ModelState);
        }
        
        var createdUserEntity = mapper.Map<NewUserDto, UserEntity>(newUserDto);
        
        var insertedUser = userRepository.Insert(createdUserEntity);
        
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = insertedUser.Id },
            insertedUser.Id);
    }
    
    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] PutUserDto? putUserDto)
    {
        if (putUserDto == null || userId == Guid.Empty)
            return BadRequest();
        
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        var existingUser = userRepository.FindById(userId);
        var userExistedBefore = existingUser != null;

        var userEntity = new UserEntity(userId);;
        userEntity = mapper.Map(putUserDto, userEntity);

        userRepository.UpdateOrInsert(userEntity, out var isInserted);
        
        if (!userExistedBefore)
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id);
        
        return NoContent();
    }

    private bool CheckKeyIsValid(string key)
    {
        foreach (var el in key)
        {
            if (!char.IsLetterOrDigit(el))
                return false;
        }
        return true;
    }
}