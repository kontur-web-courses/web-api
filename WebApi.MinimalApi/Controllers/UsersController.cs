using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;



namespace WebApi.MinimalApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        private readonly IUserRepository _userRepository;
        private IMapper mapper;

        public UsersController(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            this.mapper = mapper;
        }

        [HttpGet("{userId}")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = _userRepository.FindById(userId);
            if (user == null)
            {
                return NotFound();
            }

            var userDto = mapper.Map<UserDto>(user);



            return Ok(userDto);
        }

        

        [HttpPost]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] guid guid)
        {



            var userEntity = mapper.Map<UserEntity>(guid);

            if (guid == null)
            {
                return BadRequest();
            }

            if (guid.Login == null)
            {
                ModelState.AddModelError("login", "massege");
                var response = UnprocessableEntity(ModelState);
                return response;
            }

            if (!guid.Login.All(char.IsLetterOrDigit))
            {
                ModelState.AddModelError("login", "massege");
                var response = UnprocessableEntity(ModelState);
                return response;
            }

            _userRepository.Insert(userEntity);

            return CreatedAtAction(nameof(GetUserById), new { userId = userEntity.Id, login = userEntity.Login }, guid);
        }
    }

    
}
