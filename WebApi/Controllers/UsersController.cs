using System;
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
        public IUserRepository userRepository;
        public IMapper mapper;
        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public UsersController(IUserRepository userRepository, IMapper mapper)
        {
            this.userRepository = userRepository;
            this.mapper = mapper;
        }

        [HttpGet("{userId}", Name = nameof(GetUserById))]
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
            var userDto = mapper.Map<UserEntity>(user);
            var createdUser = userRepository.Insert(userDto);
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUser.Id },
                createdUser.Id);
        }
    }
}