using System;
using System.Linq;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.Http;
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

        [HttpPatch("{userId}")]
        public IActionResult PartiallyUpdateUser()
        {
            return Ok();
        }
    }
}