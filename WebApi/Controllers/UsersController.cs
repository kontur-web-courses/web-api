using System;
using System.Linq;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Produces("application/json", "application/xml")]
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
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return NotFound();
            }
            
            var user = userRepository.FindById(userId);

            if (user is null)
            {
                return NotFound();
            }
            
            return Ok(mapper.Map<UserEntity, UserDto>(user));
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] CreateUserDto user)
        {
            if (user is null)
            {
                return BadRequest();
            }

            if (string.IsNullOrEmpty(user.Login) || user.Login.Any(c => !char.IsLetterOrDigit(c)))
            {
                ModelState.AddModelError("Login", "Логин должен состоять только из цифр и букв");
            }

            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }
            
            var createdUserEntity = userRepository.Insert(
                mapper.Map<CreateUserDto, UserEntity>(user)
                );
            
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
        }
    }
}