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
        public IUserRepository userRepository;
        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public UsersController(IUserRepository userRepository, IMapper mapper)
        {
            this.userRepository = userRepository;
        }

        [HttpGet("{userId}")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId);
            if (user == null)
                return NotFound();
            var u = new UserDto();
            u.FullName = $"{user.LastName} {user.FirstName}";
            u.CurrentGameId = user.CurrentGameId;
            u.Id = user.Id;
            u.Login = user.Login;
            u.GamesPlayed = user.GamesPlayed;
            return Ok(u);
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] CreateDtoUser user)
        {
            
            userRepository.Insert(user);
            //return default;
            throw new NotImplementedException();
        }
    }
}