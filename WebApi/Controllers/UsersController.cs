using System;
using System.Linq;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.JsonPatch;
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
            if (user == null)
                return NotFound();
            
            return Ok(mapper.Map<UserDto>(user));
        }

        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] UserForCreationDto user)
        {
            if (user == null)
                return BadRequest();
            
            if (string.IsNullOrEmpty(user.Login) || !user.Login.All(char.IsLetterOrDigit))
                ModelState.AddModelError(nameof(UserForCreationDto.Login), "Логин может состоять только из букв и цифр");
            
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            
            var createdUserEntity = userRepository.Insert(mapper.Map<UserEntity>(user));
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id }, 
                createdUserEntity.Id);
        }

        [HttpPut("{userId}")]
        [Consumes("application/json")]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UserForUpdateDto user)
        {
            if (user == null || userId == Guid.Empty)
                return BadRequest();

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var newUserEntity = new UserEntity(userId);
            mapper.Map(user, newUserEntity);
            userRepository.UpdateOrInsert(newUserEntity, out var isInserted);

            if (isInserted)
            {
                return CreatedAtRoute(
                    nameof(GetUserById),
                    new { userId = newUserEntity.Id },
                    newUserEntity.Id);
            }
            return NoContent();
        }

        [HttpPatch("{userId}")]
        [Consumes("application/json")]
        [Produces("application/json", "application/xml")]
        public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, 
            [FromBody] JsonPatchDocument<UserForUpdateDto> patchDoc)
        {
            if (userRepository.FindById(userId) == null)
                return NotFound();
            
            var updateDto = new UserForUpdateDto();
            patchDoc.ApplyTo(updateDto, ModelState);
            var updatedUserEntity = new UserEntity(userId);
            mapper.Map(updateDto, updatedUserEntity);
            
            userRepository.Update(updatedUserEntity);
            return NoContent();
        }
    }
}