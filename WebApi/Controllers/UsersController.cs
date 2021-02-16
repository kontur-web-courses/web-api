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
        private readonly IUserRepository userRepository;
        private readonly IMapper mapper;
        
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
            var userFromRepo = userRepository.FindById(userId);
            if (userFromRepo == null)
                return NotFound();

            var userDto = mapper.Map<UserDto>(userFromRepo);
            return Ok(userDto);
        }

        [HttpPost]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] UserToCreateDto user)
        {
            if (user == null)
                return BadRequest();

            if (string.IsNullOrEmpty(user.Login) || !user.Login.All(char.IsLetterOrDigit))
            {
                ModelState.AddModelError(nameof(UserToCreateDto.Login),
                    "Login should contain only letters or digits.");
            }

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var userEntity = mapper.Map<UserEntity>(user);
            var createdUserEntity = userRepository.Insert(userEntity);

            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
        }
        
        [HttpPut("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser([FromRoute] Guid userId,
            [FromBody] UserToUpdateDto user)
        {
            if (user == null || userId == Guid.Empty)
                return BadRequest();

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var newUserEntity = new UserEntity(userId);
            mapper.Map(user, newUserEntity);
            userRepository.UpdateOrInsert(newUserEntity, out bool isInserted);

            if (isInserted)
            {
                return CreatedAtRoute(
                    nameof(GetUserById),
                    new { userId = newUserEntity.Id },
                    newUserEntity.Id);
            }
            return NoContent();
        }
    }
}