using System.Text.RegularExpressions;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly Regex regex= new Regex(@"^[a-zA-Z0-9]+$");


    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    public UsersController(IUserRepository userRepository,  IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user == null) return NotFound();

        var userDto = _mapper.Map(user, new UserDto());
        return Ok(userDto);
    }
    
    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] UserCreateDto user)
    {
        if (user == null) return BadRequest();
        
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        if (!regex.IsMatch(user.Login))
        {
            ModelState.AddModelError("Login", "Login should be number or char");
            return UnprocessableEntity(ModelState);
        }
        
        var createdUserEntity = _userRepository.Insert(_mapper.Map(user, new UserEntity()));
        var createdUserDto = _mapper.Map<UserDto>(createdUserEntity);
        
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserDto);
    }
}