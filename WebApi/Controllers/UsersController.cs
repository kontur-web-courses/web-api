using System;
using System.Linq;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
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

            if (user == null)
            {
                return NotFound();
            }

            
            return Ok(mapper.Map<UserDto>(user));
        }
        
        [HttpPost]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] NewUserData user)
        {
            if (user == null)
            {
                return BadRequest();
            }
            
            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }
            
            if (user.Login.Any(c => !char.IsLetterOrDigit(c)))
            {
                ModelState.AddModelError("Login", "Invalid login: must contains only letters or digits");
                return UnprocessableEntity(ModelState);
            }
            
            var userToSave = mapper.Map<UserEntity>(user);
            
            var savedUser = userRepository.Insert(userToSave);
            
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = savedUser.Id },
                savedUser.Id);
        }
    }
}