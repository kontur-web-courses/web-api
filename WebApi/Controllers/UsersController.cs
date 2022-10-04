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
        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        private IUserRepository _userRepository;
        private IMapper _mapper;
        public UsersController(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = _userRepository.FindById(userId);
            if (user is null)
                return NotFound();
            return Ok(_mapper.Map<UserDto>(user));
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] CreatedUserDto user)
        {
            if (user is null)
                return BadRequest();
            
            if (user.Login is null || user.Login.Any(loginChar => !char.IsLetterOrDigit(loginChar)))
            {
                ModelState.AddModelError(nameof(user.Login), "Логин должен состоять только из букв и цифр");
            }
            
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            
            var userEntity = _mapper.Map<UserEntity>(user);
            var createdUserEntity = _userRepository.Insert(userEntity);
            
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
        }
    }
}