using System;
using System.Linq;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.JsonPatch;
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
            var userFromRepository = userRepository.FindById(userId);
            if (userFromRepository == null)
                return NotFound();

            var userDto = mapper.Map<UserDto>(userFromRepository);
            return Ok(userDto);
        }

        [HttpPost]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] UserToCreate user)
        {
            if (user == null)
                return BadRequest();
            
            if (string.IsNullOrEmpty(user.Login) || !user.Login.All(char.IsLetterOrDigit))
                ModelState.AddModelError(nameof(UserToCreate.Login),"Логин должен содержать только буквы или цифры");

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            
            var userEntity = mapper.Map<UserEntity>(user);
            var createdUserEntity = userRepository.Insert(userEntity);
            
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id},
                createdUserEntity.Id);
        }

        [HttpPut("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UserToUpdate user)
        {
            if (user == null || userId == Guid.Empty)
                return BadRequest();

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var userEntity = new UserEntity(userId);
            mapper.Map(user, userEntity);
            userRepository.UpdateOrInsert(userEntity, out bool isInserted);

            if (isInserted)
            {
                return CreatedAtRoute(
                    nameof(GetUserById),
                    new {userId = userEntity.Id},
                    userEntity.Id);
            }

            return NoContent();
        }

        [HttpPatch("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserToUpdate> patchDoc)
        {
            if (patchDoc == null)
                return BadRequest();
            
            var userFromRepository = userRepository.FindById(userId);
            if (userFromRepository == null)
                return NotFound();

            var user = mapper.Map<UserToUpdate>(userFromRepository);

            patchDoc.ApplyTo(user, ModelState);
            TryValidateModel(user);

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            mapper.Map(user, userFromRepository);
            userRepository.Update(userFromRepository);

            return NoContent();
        }
    }
}