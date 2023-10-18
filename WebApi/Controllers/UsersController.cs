using System;
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
        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public UsersController(IUserRepository userRepository)
        {
            this.userRepository = userRepository;
        }

        [HttpGet("{userId}")]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var userDto = new UserDto();
            var userEntity = userRepository.FindById(userId);
            if (userEntity is null)
                return NotFound();
            userDto.Id = userEntity.Id;
            userDto.GamesPlayed = userEntity.GamesPlayed;
            userDto.CurrentGameId = userEntity.CurrentGameId;
            userDto.Login = userEntity.Login;
            userDto.FullName = userEntity.FirstName + userEntity.LastName;
            return Ok(userDto);
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] object user)
        {
            throw new NotImplementedException();
        }
    }
}