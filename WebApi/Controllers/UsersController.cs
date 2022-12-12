using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;
using System.Linq;
using System.ComponentModel;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public IUserRepository userRepository;
        public IMapper mapper;

        public UsersController(IUserRepository userRepository, IMapper mapper)
        {
            this.mapper = mapper;
            this.userRepository = userRepository;
        }

        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var userEntity = userRepository.FindById(userId);
            if(userEntity is null)
            {
                return NotFound();
            }
            
            var dto = mapper.Map<UserDto>(userEntity);

            return Ok(dto);
        }


        [HttpPost]
        public IActionResult CreateUser([FromBody] PostDto user)
        {

            if (user is null)
                return BadRequest();
            if (user.Login is null || user.Login.Any(c => !char.IsLetterOrDigit(c)))
            {
                ModelState.AddModelError("Login", "Login должен состоять только из букв из цифр");
            }
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            var createdUserEntity = mapper.Map<UserEntity>(user);
            var userEntity = userRepository.Insert(createdUserEntity);
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id);
        }
        [HttpPut("{userId}")]
        public IActionResult UpdateUser([FromBody] PutDto user, [FromRoute] Guid userId)
        {
            if (user is null)
                return BadRequest();
            if (user.Login is null)
                ModelState.AddModelError("Login", "Login не должен быть null");
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            if (user.FirstName is null || user.LastName is null)
                return UnprocessableEntity(ModelState);
            var userEntity = new UserEntity(userId)
            {
            };
            mapper.Map(user, userEntity);
            userRepository.UpdateOrInsert(userEntity, out var isInserted);
            if (isInserted)
                return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id);
            return NoContent();
        }
    }

    public class PutDto
    {
        [Required]
        [RegularExpression("^[0-9\\p{L}]*$", ErrorMessage = "Login should contain only letters or digits")]
        public string Login;
        public string FirstName;
        public string LastName;
        public Guid UserId;
    }

    public class PostDto
    {
        [Required]
        public string Login;
        [DefaultValue("John")]
        public string FirstName;
        [DefaultValue("Doe")]
        public string LastName;
    }
}