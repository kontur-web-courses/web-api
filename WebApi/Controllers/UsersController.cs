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

        public UsersController(IUserRepository userRepository, IMapper mapper)
        {
            this.userRepository = userRepository;
            this.mapper = mapper;
        }

        [HttpHead("{userId}")]
        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId);
            if (user is null) return NotFound();
            return Ok(mapper.Map<UserDto>(user));
        }

        [HttpPost]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] UserToCreateDto user)
        {
            if (user is null)
            {
                return BadRequest();
            }
            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }
            if (!user.Login.All(char.IsLetterOrDigit))
            {
                ModelState.AddModelError("Login", "Login should contain only letters or digits");
                return UnprocessableEntity(ModelState);
            }
            var createdUserEntity = userRepository.Insert(mapper.Map<UserEntity>(user));
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id
            );
        }

        [HttpPut("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser([FromRoute] Guid? userId, [FromBody] UserToUpdateDto user)
        {
            if (user is null || userId is null)
            {
                return BadRequest();
            }
            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }
            user.Id = userId.Value;
            userRepository.UpdateOrInsert(mapper.Map<UserEntity>(user), out var isInserted);
            return !isInserted
                ? NoContent()
                : CreatedAtRoute(
                      nameof(GetUserById),
                      new { userId = userId },
                      userId
                  );
        }

        [HttpPatch("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserToUpdateDto> patchDoc)
        {
            if (patchDoc is null) return BadRequest();
            var user = userRepository.FindById(userId);
            if (user is null) return NotFound();
            var updateDto = mapper.Map<UserToUpdateDto>(user);
            patchDoc.ApplyTo(updateDto, ModelState);
            TryValidateModel(updateDto);
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);
            var newUser = mapper.Map<UserEntity>(updateDto);
            userRepository.Update(newUser);
            return NoContent();
        }

        [HttpDelete("{userId}")]
        public ActionResult<UserDto> DeleteUser([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId);
            if (user is null) return NotFound();
            userRepository.Delete(userId);
            return NoContent();
        }

        [HttpOptions]
        public ActionResult<UserDto> GetOptions()
        {
            Response.Headers.Add("Allow", new[] { "POST", "GET", "OPTIONS" });
            return Ok();
        }
    }
}