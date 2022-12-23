using System;
using System.Linq;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
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
                ModelState.AddModelError("Login", "Invalid login");
            
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var user = mapper.Map<UserEntity>(userDto);
            var userEntity = userRepository.Insert(user);
            
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id);
        }
    }
}