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
    public class UsersController : Controller
    {
        private IUserRepository userRepository;

        private IMapper mapper;
        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
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
            {
                return NotFound();
            }

            var userDto = mapper.Map<UserDto>(user);
            return Ok(userDto);
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] UserCreationDto user)
        {
            if (user == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }
            // if (String.IsNullOrEmpty(user.Login) || user.Login.Any(c => !Char.IsLetterOrDigit(c)))
            if (user.Login.Any(c => !Char.IsLetterOrDigit(c)))
            {
                ModelState.AddModelError("login", "Некорректный логин");
                return UnprocessableEntity(ModelState);
            }
            
            
            var userEntity = mapper.Map<UserEntity>(user);
            var createdUserEntity = userRepository.Insert(userEntity);
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
        }

        [HttpPut("{userId}")]
        public ActionResult<UserEntity> UpdateUser([FromRoute] string userId, [FromBody] UserUpdateDto user)
        {
            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            var guid = Guid.Parse(userId);
            UserEntity newUser = new UserEntity(guid);
            mapper.Map(user, newUser);
            
            userRepository.UpdateOrInsert(newUser, out bool inserted);

            if (inserted)
            {
                return newUser;
            }
            else
            {
                return Ok();
            }

        }
    }
}