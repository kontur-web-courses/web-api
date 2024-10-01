using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
using System;

namespace WebApi.MinimalApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        private readonly IUserRepository _userRepository;

        public UsersController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet("{userId}")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = _userRepository.FindById(userId);
            if (user == null)
            {
                return NotFound();
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                Login = user.Login,
                FullName = user.LastName + ' ' + user.FirstName,
                GamesPlayed = user.GamesPlayed,
                CurrentGameId = user.CurrentGameId
            };



            return Ok(userDto);
        }

        public class CreateUserDto
        {
            public string Login { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] UserDto userDto)
        {
            if (userDto == null)
            {
                return BadRequest("User data is required.");
            }

            var userEntity = new UserEntity
            {
                Login = userDto.Login,
            };

            _userRepository.Insert(userEntity);

            return CreatedAtAction(nameof(GetUserById), new { userId = userEntity.Id }, userDto);
        }
    }
}
