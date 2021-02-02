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
        public UsersController(IUserRepository userRepository)
        {
        }

        [HttpGet("{userId}")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] object user)
        {
            throw new NotImplementedException();
        }
    }
}