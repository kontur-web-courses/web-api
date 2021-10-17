using System;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [Produces("application/json", "application/xml")]
    [ApiController]
    public class UsersController : Controller
    {
        private IUserRepository userRepository;
        private IMapper mapper;
        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public UsersController(IUserRepository userRepository, IMapper mapper)
        {
            this.userRepository = userRepository;
            this.mapper = mapper;
        }

        [HttpGet("{userId}")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var src = userRepository.FindById(userId);
            if (src == null)
                return NotFound();
            var user = mapper.Map<UserEntity, UserDto>(src);
            return Ok(user);
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] object user)
        {
            throw new NotImplementedException();
        }
    }
}