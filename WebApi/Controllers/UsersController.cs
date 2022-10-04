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
        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public IUserRepository userRepository;
        
        public UsersController(IUserRepository userRepository)
        {
            this.userRepository = userRepository;
        }

        [HttpGet("{userId}")]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var userEntity = userRepository.FindById(userId);
            if(userEntity is null)
            {
                return NotFound();
            }

            var dto = new UserDto()
            {
                Id = userEntity.Id,
                FullName = $"{userEntity.LastName} {userEntity.FirstName}",
                GamesPlayed = userEntity.GamesPlayed,
                CurrentGameId = userEntity.CurrentGameId,
                Login = userEntity.Login
            };
            return Ok(dto);
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] object user)
        {
            throw new NotImplementedException();
        }
    }
}