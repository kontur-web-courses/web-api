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
            var user = userRepository.FindById(userId);
            if (user is null)
                return NotFound();
            var userDto = mapper.Map<UserDto>(user);
            return Ok(userDto);
        }

        [HttpPost]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] UserToCreateDto userDto)
        {
            if (userDto is null)
                return BadRequest();

            if (string.IsNullOrEmpty(userDto.Login) || !userDto.Login.All(char.IsLetterOrDigit))
                ModelState.AddModelError("Login", "Login should contain only letters or digits");
            
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var user = mapper.Map<UserEntity>(userDto);
            var userEntity = userRepository.Insert(user);
            
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id);
        }

        [HttpPut("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UserToUpdateDto userDto)
        {
            if (userDto is null || userId == Guid.Empty)
                return BadRequest();
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var user = new UserEntity(userId);
            mapper.Map(userDto, user);
            
            userRepository.UpdateOrInsert(user, out var isInserted);
            
            if (!isInserted)
                return NoContent();
            
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = user.Id },
                user.Id);
        }
        
        [HttpPatch("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, 
            [FromBody] JsonPatchDocument<UserToUpdateDto> patchDoc)
        {
            if (patchDoc is null)
                return BadRequest();
            
            var user = userRepository.FindById(userId);
            
            if (user is null)
                return NotFound();

            var userDto = new UserToUpdateDto();
            patchDoc.ApplyTo(userDto, ModelState);
            TryValidateModel(userDto);
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            
            mapper.Map(userDto, user);
            userRepository.Update(user);
            
            return NoContent();
        }
        
        [HttpDelete("{userId}")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId);
            if (user is null)
                return NotFound();
            userRepository.Delete(userId);
            return NoContent();
        }
    }
}