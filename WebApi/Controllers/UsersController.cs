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
        private readonly IUserRepository userRepository;

        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public UsersController(IUserRepository userRepository)
        {
            this.userRepository = userRepository;
        }

        [HttpGet("{userId}")]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId);
            if (user is null) return NotFound();
            return Ok(new UserDto
            {
                FullName = user.FirstName + ' ' + user.LastName,
                CurrentGameId = user.CurrentGameId,
                GamesPlayed = user.GamesPlayed,
                Id = user.Id,
                Login = user.Login
            });
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] object user)
        {
            throw new NotImplementedException();
        }
    }
}