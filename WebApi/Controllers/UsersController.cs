using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        private readonly IUserRepository userRepository;
        private readonly IMapper userMapper;
        private readonly LinkGenerator generator;

        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public UsersController(IUserRepository userRepository, IMapper userMapper, LinkGenerator generator)
        {
            this.userRepository = userRepository;
            this.userMapper = userMapper;
            this.generator = generator;
        }

        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [HttpHead("{userId}")]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var userEntity = userRepository.FindById(userId);
            if (userEntity == null)
                return NotFound();
            return Ok(userMapper.Map<UserDto>(userEntity));
        }

        [HttpPost]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] UserCreateDto userCreateDto)
        {
            if (userCreateDto == null)
            {
                return BadRequest();
            }

            if (string.IsNullOrEmpty(userCreateDto.Login) || !userCreateDto.Login.All(char.IsLetterOrDigit))
            {
                ModelState.AddModelError(nameof(userCreateDto.Login), "Логин должен состоять из букв и цифр");
            }
            
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            
            var user = userMapper.Map<UserEntity>(userCreateDto);
            var createdUser = userRepository.Insert(user);
            
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUser.Id },
                createdUser.Id);
        }
        
        [HttpPut("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UserUpdateDto userUpdateDto)
        {
            if (userUpdateDto == null || Guid.Empty == userId)
                return BadRequest();

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var user = userMapper.Map(userUpdateDto, new UserEntity(userId));
            userRepository.UpdateOrInsert(user, out var isInserted);

            if (isInserted)
            {
                return CreatedAtRoute(
                    nameof(GetUserById),
                    new { userId = user.Id },
                    user.Id);
            }

            return NoContent();
        }

        [HttpPatch("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserUpdateDto> patchDoc)
        {
            if (patchDoc == null)
                return BadRequest();
            
            var user = userRepository.FindById(userId);
            
            if (user == null)
                return NotFound();

            var userUpdateDto = userMapper.Map<UserUpdateDto>(user);
            patchDoc.ApplyTo(userUpdateDto, ModelState);

            TryValidateModel(userUpdateDto);
            
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            
            userMapper.Map(userUpdateDto, user);
            userRepository.Update(user);

            return NoContent();
        }
        
        [HttpDelete("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            if (userRepository.FindById(userId) == null) 
                return NotFound();
            
            userRepository.Delete(userId);
            return NoContent();
        }

        [HttpGet]
        [Produces("application/json", "application/xml")]
        public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery]int pageSize = 10)
        {
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            pageSize = pageSize < 1 ? 1 : pageSize > 20 ? 20 : pageSize;
            
            var pageList = userRepository.GetPage(pageNumber, pageSize);
            var users = userMapper.Map<IEnumerable<UserDto>>(pageList);
            
            var paginationHeader = new
            {
                previousPageLink = pageList.HasPrevious 
                    ? generator.GetUriByAction(
                        HttpContext, 
                        nameof(GetUsers), 
                        values: new {pageNumber = pageList.CurrentPage - 1, pageSize = pageList.PageSize}) 
                    : null,
                
                nextPageLink = pageList.HasNext 
                    ? generator.GetUriByAction(
                        HttpContext, 
                        nameof(GetUsers), 
                        values: new {pageNumber = pageList.CurrentPage + 1, pageSize = pageList.PageSize})
                    : null,
                
                totalCount = pageList.TotalCount,
                pageSize = pageList.PageSize,
                currentPage = pageList.CurrentPage,
                totalPages = pageList.TotalPages,
            };
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
            
            return Ok(users);
        }


        [HttpOptions]
        public IActionResult GetUsersOptions()
        {
            Response.Headers.Add("Allow", "POST,GET,OPTIONS");
            return Ok();
        }
    }
}