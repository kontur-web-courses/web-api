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
        private IUserRepository _userRepository;
        private IMapper _userMapper;

        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public UsersController(IUserRepository userRepository, IMapper userMapper)
        {
            _userRepository = userRepository;
            _userMapper = userMapper;
        }

        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = new UserDto();
            var userEntity = _userRepository.FindById(userId);
            if (userEntity == null)
                return NotFound();
            /*
            user.Id = userId;
            user.Login = userEntity.Login;
            user.FullName = $"{userEntity.LastName} {userEntity.FirstName}";
            user.GamesPlayed = userEntity.GamesPlayed;
            user.CurrentGameId = userEntity.CurrentGameId;
            */
            return Ok(_userMapper.Map<UserDto>(userEntity));
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] UserCreationDto user)
        {
            if (user == null)
                return BadRequest();
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            if (user.Login.Where(c => !char.IsLetterOrDigit(c)).Any())
            {
                ModelState.AddModelError("Login", "Requires both letters and digits only");
                return UnprocessableEntity(ModelState);
            }   
            var userEntity = _userMapper.Map<UserEntity>(user);
            userEntity = _userRepository.Insert(userEntity);
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id);
        }
    }
}