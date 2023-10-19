using System;
using System.Linq;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework.Constraints;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public UsersController(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }
            
        [Produces("application/json", "application/xml")]
        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [HttpHead("{userId}")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = _userRepository.FindById(userId);
            return (user is null) ? NotFound() : Ok(_mapper.Map<UserDto>(user));
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] UserCreateDto user)
        {
            if (user is null)
                return BadRequest();
        
            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }
        
            var createdUser = _userRepository.Insert(_mapper.Map<UserEntity>(user));
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUser.Id },
                createdUser.Id);
        }
        
        [Produces("application/json", "application/xml")]
        [HttpPut("{userId}")]
        public IActionResult UpdateUser([FromBody] UserPutDto user, [FromRoute] Guid userId)
        {
            if (user is null || userId == Guid.Empty)
            {
                return BadRequest();
            }
            
            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }
            
            var newUserEntity = new UserEntity(userId);
            _mapper.Map(user, newUserEntity);
            _userRepository.UpdateOrInsert(newUserEntity, out bool isInserted);

            if (!isInserted)
                return NoContent();
            
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = newUserEntity.Id },
                newUserEntity.Id);
        }
        
        [Produces("application/json", "application/xml")]
        [HttpPatch("{userId}")]
        public IActionResult PartiallyUpdateUser([FromBody] JsonPatchDocument<UserPutDto> patchDoc, [FromRoute] Guid userId)
        {
            if (patchDoc is null)
            {
                return BadRequest();
            }
            var userEntity = _userRepository.FindById(userId);
            if (userEntity is null)
            {
                return NotFound();
            }
            
            
            var updateDto = _mapper.Map<UserPutDto>(userEntity);
           
            patchDoc.ApplyTo(updateDto, ModelState);

            if (updateDto is null || userId == Guid.Empty)
            {
                return NotFound();
            }
            if (!TryValidateModel(updateDto))
            {
                return UnprocessableEntity(ModelState);
            }

            return NoContent();
        }
        
        [Produces("application/json", "application/xml")]
        [HttpDelete("{userId}")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            var userEntity = _userRepository.FindById(userId);
            
            if (userEntity is null)
            {
                return NotFound();
            }
            
            _userRepository.Delete(userId);
            return NoContent();
        }
        
        // [Produces("application/json", "application/xml")]
        // [HttpHead("{userId}")]
        // public IActionResult DeleteUser([FromRoute] Guid userId)
        // {
        //     var userEntity = _userRepository.FindById(userId);
        //     
        //     if (userEntity is null)
        //     {
        //         return NotFound();
        //     }
        //     
        //     _userRepository.Delete(userId);
        //     return NoContent();
        // }
    }
}