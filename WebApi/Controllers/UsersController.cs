using System;
using System.Linq;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [Produces("application/json", "application/xml")]
    [ApiController]
    public class UsersController : Controller
    {
        private IUserRepository userRepository;
        private IMapper mapper;

        public UsersController(IUserRepository userRepository, IMapper mapper)
        {
            this.userRepository = userRepository;
            this.mapper = mapper;
        }
        
        [HttpHead("{userId}")]
        [HttpGet("{userId}", Name = nameof(GetUserById))]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var src = userRepository.FindById(userId);
            if (src == null)
                return NotFound();
            var user = mapper.Map<UserEntity, UserDto>(src);
            return Ok(user);
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] CreateUserDto user)
        {
            if (user == null)
                return BadRequest();
            if (user?.Login != null && !user.Login.ToCharArray().All(char.IsLetterOrDigit))
                ModelState.AddModelError("Login", "Логин должен состоять из букв или цифр");
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            var userEntity = mapper.Map<CreateUserDto, UserEntity>(user);
            var createdUserEntity = userRepository.Insert(userEntity);
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
        }

        [HttpPut("{userId}")]
        public IActionResult UpdateUser([FromRoute] string userId, [FromBody] UpdateUserDto user)
        {
            if (user == null || !Guid.TryParse(userId, out var id))
                return BadRequest();
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            var userEntity = mapper.Map<UpdateUserDto, UserEntity>(user, new UserEntity(id));
            userRepository.UpdateOrInsert(userEntity, out var isInserted);
            if (isInserted) 
                return CreatedAtRoute(
                    nameof(GetUserById),
                    new { userId = id },
                    id);
            return NoContent();
        }

        [HttpDelete("{userId}")]
        public IActionResult DeleteUser([FromRoute] string userId)
        {
            if (!Guid.TryParse(userId, out var id))
                return NotFound();
            if (userRepository.FindById(id) == null)
                return NotFound();
            userRepository.Delete(id);
            return NoContent();
        }
    }
}