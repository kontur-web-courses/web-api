using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        private IUserRepository userRepository;
        private IMapper mapper;
        public UsersController(IUserRepository userRepository, IMapper mapper)
        {
            this.userRepository = userRepository;
            this.mapper = mapper;
        }

        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var src = userRepository.FindById(userId);
            if (src is null)
            {
                return NotFound();
            }
            var user = mapper.Map<UserDto>(src);
            return user;
        }

        [HttpPost]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] UserInfo user)
        {
            if (user is null)
            {
                return BadRequest();
            }
            if (!IsLoginValid(user.Login))
            {
                ModelState.AddModelError("Login", "Login should contain only letters or digits");
                return UnprocessableEntity(ModelState);
            };
            var createdUserEntity = mapper.Map<UserEntity>(user);
            createdUserEntity = userRepository.Insert(createdUserEntity);
            var result = CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
            return result;

        }

        private bool IsLoginValid(string login)
        {
            return login != null && login.All(char.IsLetterOrDigit);
        }
    }

    public class UserInfo
    {
        [Required]
        public string Login { get; set; }
        public string FirstName{ get; set; }
        public string LastName{ get; set; }
    }
}