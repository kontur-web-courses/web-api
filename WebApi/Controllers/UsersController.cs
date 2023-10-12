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
        private readonly IUserRepository _userRepository;
        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public UsersController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }
            
        [Produces("application/json", "application/xml")]
        [HttpGet("{userId}")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = _userRepository.FindById(userId);
            return (user is null) ? NotFound() : Ok(new UserDto(){Id = user.Id, Login = user.Login, FullName = $"{user.LastName} {user.FirstName}", GamesPlayed = user.GamesPlayed, CurrentGameId = user.CurrentGameId});
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] object user)
        {
            throw new NotImplementedException();
        }
    }
}