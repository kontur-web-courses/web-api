using System;
using System.Linq;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json", "application/xml")]

    public class UsersController : Controller
    {
        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public UsersController(IUserRepository userRepository, IMapper mapper)
        {
            this.userRepository = userRepository;
            this.mapper = mapper;
        }
        
        [HttpGet("{userId}", Name = nameof(GetUserById))]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return NotFound();
            }

            var user = userRepository.FindById(userId);
            if (user is null)
            {
                return NotFound();
            }

            return Ok(mapper.Map<UserEntity, UserDto>(user));
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] CreateUserDto? userDto)
        {
            if (userDto is null)
            {
                return BadRequest();
            }
            if (string.IsNullOrEmpty(userDto.Login) || userDto.Login.Any(x=>!char.IsLetterOrDigit(x)))
            {
                ModelState.AddModelError("Login", "ты писать н еумеешь");
            }
            
            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }
            
            var userEntity = mapper.Map<CreateUserDto, UserEntity>(userDto);
            var newUser = userRepository.Insert(userEntity);
            
            return CreatedAtRoute(nameof(GetUserById), new { userId = newUser.Id }, newUser.Id);
        }

        private readonly IUserRepository userRepository;
        private readonly IMapper mapper;
    }
}