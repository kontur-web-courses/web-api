using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
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
            if (user == null) return NotFound();

            return Ok(mapper.Map<UserDto>(user));
        }

        [HttpPost]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] UserToCreate userToCreate)
        {
            if (userToCreate == null)
                return BadRequest();
            
            if (string.IsNullOrEmpty(userToCreate.Login) || !userToCreate.Login.All(char.IsLetterOrDigit))
            {
                ModelState.AddModelError(nameof(UserToCreate.Login), "Логин должен состоять из букв и цифр");
            }
            
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            
            var user = mapper.Map<UserEntity>(userToCreate);
            var createdUser = userRepository.Insert(user);
            
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUser.Id },
                createdUser.Id);
        }

        [HttpPut("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UserToUpdate userToUpdate)
        {
            if (userToUpdate == null || Guid.Empty == userId)
                return BadRequest();
            
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            
            var user = mapper.Map(userToUpdate, new UserEntity(userId));
            userRepository.UpdateOrInsert(user, out var isInserted);
            
            if (isInserted)
            {
                return CreatedAtRoute(
                    nameof(GetUserById),
                    new { userId = user.Id },
                    user.Id);
            }

            return NoContent();
        }
    }

    public class UserToUpdate
    {
        [Required]
        [RegularExpression("^[0-9\\p{L}]*$", ErrorMessage = "Login should contain only letters or digits")]
        public string Login { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
    }
    
    public class UserToCreate
    {
        [Required]
        public string Login { get; set; }
        [DefaultValue("John")]
        public string FirstName { get; set; }
        [DefaultValue("Doe")]
        public string LastName { get; set; }
    }
}