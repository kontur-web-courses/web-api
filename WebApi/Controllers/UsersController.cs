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
    [Produces("application/json", "application/xml")]
    public class UsersController : Controller
    {
        private readonly IUserRepository repository;
        private readonly IMapper mapper;

        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public UsersController(IUserRepository repository, IMapper mapper)
        {
            this.repository = repository;
            this.mapper = mapper;
        }

        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [HttpHead("{userId}")]
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

            var user = mapper.Map<UserEntity>(userToCreate);

            var createdUserEntity = repository.Insert(user);

            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
        }

        [HttpPut("{userId}")]
        public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UserToUpdateDto userToUpdate)
        {
            if (userToUpdate is null || userId == Guid.Empty)
                return BadRequest();

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var user = new UserEntity(userId);

            mapper.Map(userToUpdate, user);

            repository.UpdateOrInsert(user, out var isInserted);

            if (!isInserted)
                return NoContent();

            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = user.Id },
                user.Id);
        }

        [HttpPatch("{userId}")]
        public IActionResult PartiallyUpdateUser(
            [FromRoute] Guid userId,
            [FromBody] JsonPatchDocument<UserToUpdateDto> patchDoc)
        {
            if (patchDoc is null)
            {
                return BadRequest();
            }

            var user = repository.FindById(userId);
            if (user is null)
            {
                return NotFound();
            }

            var updateDto = new UserToUpdateDto();
            patchDoc.ApplyTo(updateDto, ModelState);

            if (!TryValidateModel(updateDto))
            {
                return UnprocessableEntity(ModelState);
            }

            mapper.Map(updateDto, user);
            repository.Update(user);

            return NoContent();
        }

        [HttpDelete("{userId}")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            var user = repository.FindById(userId);
            if (user is null)
            {
                return NotFound();
            }

            repository.Delete(userId);

            return NoContent();
        }
    }
}