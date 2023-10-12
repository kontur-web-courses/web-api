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
    [Produces("application/json", "application/xml")]
    public class UsersController : Controller
    {
        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public UsersController(IUserRepository userRepository, IMapper mapper)
        {
            this.userRepository = userRepository;
            this.mapper = mapper;
        }

        [HttpGet("{userId:guid}", Name = nameof(GetUserById))]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return NotFound();
            }

            var user = userRepository.FindById(userId);
            if (user is null)
            {
                return NotFound();
            }

            return Ok(mapper.Map<UserEntity, UserDto>(user));
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] CreateUserDto? userDto)
        {
            if (userDto is null)
            {
                return BadRequest();
            }

            if (string.IsNullOrEmpty(userDto.Login) || userDto.Login.Any(x => !char.IsLetterOrDigit(x)))
            {
                ModelState.AddModelError("Login", "ты писать н еумеешь");
            }

            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            var userEntity = mapper.Map<CreateUserDto, UserEntity>(userDto);
            var newUser = userRepository.Insert(userEntity);

            return CreatedAtRoute(nameof(GetUserById), new {userId = newUser.Id}, newUser.Id);
        }

        [HttpPut("{userId}")]
        public IActionResult UpdateUser([FromBody] UpdateUserDto? userDto, [FromRoute] Guid userId)
        {
            if (userDto is null || userId == Guid.Empty)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            var userEntity = mapper.Map<UpdateUserDto, UserEntity>(userDto);
            userEntity.Id = userId;
            
            userRepository.UpdateOrInsert(userEntity, out var isInserted);

            if (isInserted)
            {
                return CreatedAtRoute(nameof(GetUserById), new {userId = userId}, userId);
            }
            return NoContent();
        }


        [HttpPatch("{userId:guid}")]
        public IActionResult PartiallyUpdateUser([FromBody] JsonPatchDocument<UpdateUserDto>? patchDoc, [FromRoute] Guid userId)
        {
            if (patchDoc is null || userId == Guid.Empty)
            {
                return BadRequest();
            }
            
            var currentUser = userRepository.FindById(userId);
            if (currentUser is null)
            {
                return NotFound();
            }
            
            var updateUser = mapper.Map<UserEntity, UpdateUserDto>(currentUser);
            patchDoc.ApplyTo(updateUser, ModelState);
            
            if (!TryValidateModel(updateUser))
            {
                return UnprocessableEntity(ModelState);
            }

            var userEntity = mapper.Map<UpdateUserDto, UserEntity>(updateUser);
            
            userRepository.Update(userEntity);
            return NoContent();
        }


        

        private readonly IUserRepository userRepository;
        private readonly IMapper mapper;
    }
}