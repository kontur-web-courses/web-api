using System;
using System.Linq;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Produces("application/json", "application/xml")]
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

        [HttpGet("{userId}", Name = nameof(GetUserById))]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            if (userId == Guid.Empty)
                return NotFound();
            
            var user = userRepository.FindById(userId);

            if (user is null)
                return NotFound();
            
            return Ok(mapper.Map<UserEntity, UserDto>(user));
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] CreateUserDto user)
        {
            if (user is null)
                return BadRequest();

            if (string.IsNullOrEmpty(user.Login) || user.Login.Any(c => !char.IsLetterOrDigit(c)))
                ModelState.AddModelError("Login", "Login should contain only letters or digits");

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            
            var createdUserEntity = userRepository.Insert(
                mapper.Map<CreateUserDto, UserEntity>(user)
                );
            
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
        }

        [HttpPut("{userId}")]
        public IActionResult UpdateUser([FromBody] UpdateUserDto user, [FromRoute] Guid userId)
        {
            if (user is null || userId == Guid.Empty)
                return BadRequest();
            
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var userEntity = mapper.Map(user, new UserEntity(userId));
            
            userRepository.UpdateOrInsert(userEntity, out var isInserted);

            if (!isInserted)
                return NoContent();

            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId },
                userId);
        }

        [HttpPatch("{userId}")]
        public IActionResult PartiallyUpdateUser([FromBody] JsonPatchDocument<UpdateUserDto> patchDoc, [FromRoute] Guid userId)
        {
            if (patchDoc is null)
                return BadRequest();
            
            var currentUser = userRepository.FindById(userId);
            
            if (currentUser is null)
                return NotFound();
            
            var updateUser = mapper.Map<UserEntity, UpdateUserDto>(currentUser);
            
            patchDoc.ApplyTo(updateUser, ModelState);

            if (!TryValidateModel(updateUser))
                return UnprocessableEntity(ModelState);
            
            userRepository.Update(
                mapper.Map<UpdateUserDto, UserEntity>(updateUser)
                );

            return NoContent();
        }
    }
}