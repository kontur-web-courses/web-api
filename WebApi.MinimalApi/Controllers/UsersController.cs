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

        [HttpPost]
        public IActionResult CreateUser([FromBody] UserDto userDto)
        {
            throw new NotImplementedException();
        }
    }
}
