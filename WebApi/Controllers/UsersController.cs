using System;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        private readonly IUserRepository users;
        private readonly IMapper mapper;

        public UsersController(IUserRepository users, IMapper mapper)
        {
            this.users = users;
            this.mapper = mapper;
        }

        [HttpGet("{userId:guid}", Name = nameof(GetUserById))]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = users.FindById(userId);
            if (user is null)
                return NotFound();

            var userDto = mapper.Map<UserDto>(user);
            return Ok(userDto);
        }

        [HttpPost]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] CreateUserDto user)
        {
            if (user is null)
                return BadRequest();

            if (string.IsNullOrEmpty(user.Login) || !user.Login.All(char.IsLetterOrDigit))
            {
                ModelState.AddModelError(nameof(CreateUserDto.Login),
                    "Login should contain only letters or digits");
            }

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var userEntity = mapper.Map<UserEntity>(user);
            var createdUserEntity = users.Insert(userEntity);
            var response = CreatedAtRoute(
                nameof(GetUserById),
                new {userId = createdUserEntity.Id},
                createdUserEntity.Id);
      
            return response;
        }

        [HttpPut("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdateUserDto user)
        {
            if (user is null || userId == Guid.Empty)
                return BadRequest();
            
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            
            var userEntity = new UserEntity(userId);
            mapper.Map(user, userEntity);
            
            users.UpdateOrInsert(userEntity, out var isInserted);
            
            return isInserted 
                ? CreatedAtRoute(
                    nameof(GetUserById),
                    new {userId = userEntity.Id},
                    userEntity.Id) 
                : NoContent();
        }
        
        [HttpPatch("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc)
        {
            if (patchDoc is null)
                return BadRequest();

            var userEntity = users.FindById(userId);
            if (userEntity is null)
                return NotFound();

            var updateDto = mapper.Map<UpdateUserDto>(userEntity);
            patchDoc.ApplyTo(updateDto, ModelState);
            
            TryValidateModel(updateDto);
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            
            mapper.Map(updateDto, userEntity);
            users.Update(userEntity);

            return NoContent();
        }

        [HttpDelete("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            var user = users.FindById(userId);
            if (user is null)
                return NotFound();
            
            users.Delete(userId);
            return NoContent();
        }
    }
}