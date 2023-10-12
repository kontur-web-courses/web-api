using System;
using System.Linq;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.Mvc;
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
                // var errorList = ModelState.ToDictionary(
                //     kvp => ToCamelCase(kvp.Key),
                //     kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                // );
                //
                // return UnprocessableEntity(errorList);
                return UnprocessableEntity(ModelState);
            }

            var createdUser = _userRepository.Insert(_mapper.Map<UserEntity>(user));
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUser.Id },
                createdUser.Id);
        }
        
        private static string ToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }
            return name.Substring(0, 1).ToLower() + name.Substring(1);
        }
    }
}