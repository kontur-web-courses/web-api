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
        private IUserRepository _userRepository;
        private IMapper _userMapper;

        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public UsersController(IUserRepository userRepository, IMapper userMapper)
        {
            _userRepository = userRepository;
            _userMapper = userMapper;
        }

        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [Produces("application/json", "application/xml")]
        [HttpHead("{userId}")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = new UserDto();
            var userEntity = _userRepository.FindById(userId);
            if (userEntity == null)
                return NotFound();
            /*
            user.Id = userId;
            user.Login = userEntity.Login;
            user.FullName = $"{userEntity.LastName} {userEntity.FirstName}";
            user.GamesPlayed = userEntity.GamesPlayed;
            user.CurrentGameId = userEntity.CurrentGameId;
            */
            return Ok(_userMapper.Map<UserDto>(userEntity));
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] UserCreationDto user)
        {
            if (user == null)
                return BadRequest();
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            if (user.Login.Where(c => !char.IsLetterOrDigit(c)).Any())
            {
                ModelState.AddModelError("Login", "Requires both letters and digits only");
                return UnprocessableEntity(ModelState);
            }   
            var userEntity = _userMapper.Map<UserEntity>(user);
            userEntity = _userRepository.Insert(userEntity);
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id);
        }

        [HttpPut("{userId}")]
        public IActionResult UpdateUser([FromBody] UserUpdateDto modifiedUser, [FromRoute] Guid userId)
        {
            if (modifiedUser == null || userId == Guid.Empty)
                return BadRequest();
            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }
            var user = _userMapper.Map(modifiedUser, _userRepository.FindById(userId) ?? new UserEntity(userId));
            _userRepository.UpdateOrInsert(user, out var isInserted);
            if (isInserted)
            {
                return CreatedAtRoute(nameof(GetUserById),
                    new { userId = user.Id },
                    user.Id);
            }
            return NoContent();
        }

        [HttpPatch("{userId}")]
        public IActionResult PatchUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserUpdateDto> patchDoc)
        {
            if (patchDoc == null)
                return BadRequest();
            var user = _userRepository.FindById(userId);
            if (user == null)
                return NotFound();
            var userUpdateDto = _userMapper.Map<UserEntity, UserUpdateDto>(user);
            patchDoc.ApplyTo(userUpdateDto, ModelState);
            TryValidateModel(userUpdateDto);
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            _userRepository.Update(_userMapper.Map(userUpdateDto, user));
            return NoContent();
        }

        [HttpDelete("{userId}")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            if (userId == Guid.Empty)
                return NotFound();
            var user = _userRepository.FindById(userId);
            if (user == null)
                return NotFound();
            _userRepository.Delete(userId);
            return NoContent();
        }
    }
}