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
            if (user == null)
            {
                return NotFound();
            }

            var userDto = mapper.Map<UserDto>(user);

            return Ok(userDto);
        }

        [HttpPost]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] UserToCreateDto user)
        {
            if (user == null)
            {
                return BadRequest();
            }
            
            if (user.Login != null && !user.Login.All(char.IsLetterOrDigit))
            {
                ModelState.AddModelError("Login", "Логин состоит не только из букв и цифр");
            }
            
            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            var createdUserEntity = userRepository.Insert(mapper.Map<UserEntity>(user));

            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
        }
    }
}