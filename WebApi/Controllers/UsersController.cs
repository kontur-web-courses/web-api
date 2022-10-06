using System;
using System.Collections.Generic;
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
        [HttpHead("{userId}")]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var userEntity = userRepository.FindById(userId);
            return userEntity == null
              ? NotFound() 
                : Ok(mapper.Map<UserDto>(userEntity));
        }

        [HttpPost]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] UserCreationDto user)
        {
            if (user == null)
                return BadRequest();
            
            if (string.IsNullOrEmpty(user.Login))
            {
                ModelState.AddModelError("Login", "Gde login, Lebovski???");
                return UnprocessableEntity(ModelState);
            }
                
            
            if (!user.Login.All(char.IsLetterOrDigit))
            {
                ModelState.AddModelError("Login", "Login must consist of letters and digits only!!! tupie");
                return UnprocessableEntity(ModelState);
            }
            var userMapped = mapper.Map<UserEntity>(user);
            var userEntity = userRepository.Insert(userMapped);
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id);
        }

        [HttpPut("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser([FromBody] UserUpdateDto user, [FromRoute] Guid userId)
        {
            if (user == null || userId == Guid.Empty)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            user.Id = userId;
            var userEntity = mapper.Map<UserEntity>(user);
            userRepository.UpdateOrInsert(userEntity, out var isInserted);
            return isInserted
                ? CreatedAtRoute(
                    nameof(GetUserById),
                    new { userId = userEntity.Id },
                    userEntity.Id)
                : NoContent();
        }

        [HttpPatch("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult PartiallyUpdateUser([FromBody] JsonPatchDocument<UserUpdateDto> patchDoc, [FromRoute] Guid userId)
        {
            if (userId == Guid.Empty)
                return NotFound();
            
            if (patchDoc == null)
                return BadRequest();
            
            var updates = new UserUpdateDto();
            patchDoc.ApplyTo(updates, ModelState);
            var userEntity = userRepository.FindById(userId);
            
            if (userEntity == null)
                return NotFound();

            if (updates.Login != null)
                userEntity.Login = updates.Login;
            if (updates.FirstName != null)
                userEntity.FirstName = updates.FirstName;
            if (updates.LastName != null)
                userEntity.LastName = updates.LastName;

            var errors = GetErrors(updates);
            foreach (var error in errors)
            {
                ModelState.AddModelError(error.key, error.value);
            }

            if (errors.Any())
            {
                return UnprocessableEntity(ModelState);
            }
            
            userRepository.Update(userEntity);
            return NoContent();
        }

        [HttpDelete("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            var userEntity = userRepository.FindById(userId);
            if (userEntity == null)
            {
                return NotFound();
            }
            userRepository.Delete(userId);
            return NoContent();
        }

        private IReadOnlyCollection<(string key, string value)> GetErrors(UserUpdateDto userUpdateDto)
        {
            var errors = new List<(string, string)>();
            if (IsInvalidString(userUpdateDto.Login))
            {
                errors.Add(("login", "ah tiz nehoroshiy, login zabil"));
            }
            if (IsInvalidString(userUpdateDto.FirstName))
            {
                errors.Add(("firstName", "ah tiz nehoroshiy, firstName zabil"));
            }
            if (IsInvalidString(userUpdateDto.LastName))
            {
                errors.Add(("lastName", "ah tiz nehoroshiy, lastName zabil"));
            }

            return errors;
        }
        private void SearchForErrors(UserUpdateDto upates)
        {
            
        }

        private bool IsInvalidString(string str)
        {
            return str != null
                   && (str == string.Empty || !str.All(char.IsLetterOrDigit));
        }
    }
}