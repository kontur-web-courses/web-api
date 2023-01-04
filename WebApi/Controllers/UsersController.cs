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

        [HttpGet("{userId:guid}", Name = nameof(GetUserById))]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId);

            if (user == null)
                return NotFound();

            return Ok(mapper.Map<UserDto>(user));
        }

        [HttpPost]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] UserCreationDto userDto)
        {
            if (userDto == null)
                return BadRequest();

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            if (!userDto.Login.All(char.IsLetterOrDigit))
            {
                ModelState.AddModelError("Login", "Login should contain only letters or digits");
                return UnprocessableEntity(ModelState);
            }

            var user = mapper.Map<UserEntity>(userDto);
            user = userRepository.Insert(user);

            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = user.Id },
                user.Id);
        }

        [HttpPut("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser([FromBody] UserUpdateDto userDto, [FromRoute] Guid userId)
        {
            if (userDto == null || userId == Guid.Empty)
                return BadRequest();

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var user = mapper.Map(userDto, new UserEntity(userId));
            userRepository.UpdateOrInsert(user, out var isInserted);

            return isInserted
                ? CreatedAtRoute(
                    nameof(GetUserById),
                    new { userId = user.Id },
                    user.Id)
                : NoContent();
        }

        [HttpPatch("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult PartiallyUpdateUser([FromBody] JsonPatchDocument<UserUpdateDto> patchDoc, [FromRoute] Guid userId)
        {
            if (patchDoc == null)
                return BadRequest();

            if (userId == Guid.Empty)
                return NotFound();

            var user = userRepository.FindById(userId);
            if (user == null)
                return NotFound();

            var userDto = mapper.Map<UserUpdateDto>(user);
            patchDoc.ApplyTo(userDto, ModelState);

            if (!TryValidateModel(userDto))
                return UnprocessableEntity(ModelState);

            user = mapper.Map<UserEntity>(userDto);
            userRepository.Update(user);
            return NoContent();
        }

        [HttpDelete("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            if (userId == Guid.Empty)
                return NotFound();

            var user = userRepository.FindById(userId);

            if (user == null)
                return NotFound();

            userRepository.Delete(userId);
            return NoContent();
        }
    }
}