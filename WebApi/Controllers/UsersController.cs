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
    [Produces("application/json", "application/xml")]
    public class UsersController : Controller
    {
        private IUserRepository repository;
        private IMapper mapper;
        
        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public UsersController(IUserRepository repository, IMapper mapper)
        {
            this.repository = repository;
            this.mapper = mapper;
        }

        [HttpGet("{userId}", Name = nameof(GetUserById))]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = repository.FindById(userId);

            if (user == null)
                return NotFound();

            return Ok(mapper.Map<UserDto>(user));
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] UserToCreateDto userToCreate)
        {
            if (userToCreate is null)
            {
                return BadRequest();
            }

            if ((string.IsNullOrEmpty(userToCreate.Login)
                || !userToCreate.Login.All(c => char.IsLetterOrDigit(c))))
            {
                ModelState.AddModelError(
                    nameof(userToCreate.Login),
                    "Login should contain only letters or digits");
            }

            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            var user = this.mapper.Map<UserEntity>(userToCreate);

            var createdUserEntity = this.repository.Insert(user);

            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
        }
    }
}