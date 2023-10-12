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
        private readonly IUserRepository userRepository;
        private readonly IMapper mapper;

        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public UsersController(IUserRepository userRepository, IMapper mapper)
        {
            this.userRepository = userRepository;
            this.mapper = mapper;
        }

        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId);
            if (user is null)
                return NotFound();
            return Ok(mapper.Map<UserDto>(user));
        }

        [HttpPost]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] CreateUserDto createUserDto)
        {
            if (createUserDto is null)
                return BadRequest();

            if (createUserDto.Login is not null && !createUserDto.Login.All(char.IsLetterOrDigit))
                ModelState.AddModelError("Login", "Логин должен состоять только из символов и цифр");

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var user = userRepository.Insert(mapper.Map<UserEntity>(createUserDto));
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = user.Id },
                user.Id);
        }
    }
}