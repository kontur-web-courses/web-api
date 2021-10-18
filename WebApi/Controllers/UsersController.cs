using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
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
        private readonly IMapper mapper;
        private readonly LinkGenerator linkGenerator;

        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
        {
            this.userRepository = userRepository;
            this.mapper = mapper;
            this.linkGenerator = linkGenerator;
        }

        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [HttpHead("{userId}")]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId);
            if (user == null)
                return NotFound();
            
            return Ok(mapper.Map<UserDto>(user));
        }

        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] UserForCreationDto user)
        {
            if (user == null)
                return BadRequest();
            
            if (string.IsNullOrEmpty(user.Login) || !user.Login.All(char.IsLetterOrDigit))
                ModelState.AddModelError(nameof(UserForCreationDto.Login), "Логин может состоять только из букв и цифр");
            
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            
            var createdUserEntity = userRepository.Insert(mapper.Map<UserEntity>(user));
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id }, 
                createdUserEntity.Id);
        }

        [HttpPut("{userId}")]
        [Consumes("application/json")]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UserForUpdateDto user)
        {
            if (user == null || userId == Guid.Empty)
                return BadRequest();

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var newUserEntity = new UserEntity(userId);
            mapper.Map(user, newUserEntity);
            userRepository.UpdateOrInsert(newUserEntity, out var isInserted);

            if (isInserted)
            {
                return CreatedAtRoute(
                    nameof(GetUserById),
                    new { userId = newUserEntity.Id },
                    newUserEntity.Id);
            }
            return NoContent();
        }

        [HttpPatch("{userId}")]
        [Consumes("application/json")]
        [Produces("application/json", "application/xml")]
        public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, 
            [FromBody] JsonPatchDocument<UserForUpdateDto> patchDoc)
        {
            if (patchDoc == null)
                return BadRequest();
            
            var user = userRepository.FindById(userId);
            
            if (user == null)
                return NotFound();
            
            var updateDto = mapper.Map<UserForUpdateDto>(user);
            patchDoc.ApplyTo(updateDto, ModelState);
            
            TryValidateModel(updateDto);
            
            if (string.IsNullOrEmpty(updateDto.Login))
                ModelState.AddModelError(nameof(UserForUpdateDto.Login), "Логин не может быть пустым");

            if (string.IsNullOrEmpty(updateDto.FirstName))
                ModelState.AddModelError(nameof(UserForUpdateDto.FirstName), "Имя не может быть пустым");

            if (string.IsNullOrEmpty(updateDto.LastName))
                ModelState.AddModelError(nameof(UserForUpdateDto.LastName), "Фамилия не может быть пустой");

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            
            var updatedUserEntity = new UserEntity(userId);
            mapper.Map(updateDto, updatedUserEntity);
            
            userRepository.Update(updatedUserEntity);
            return NoContent();
        }

        [HttpDelete("{userId}")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            if (userRepository.FindById(userId) == null)
                return NotFound();
            userRepository.Delete(userId);
            return NoContent();
        }
        
        [HttpGet(Name = nameof(GetUsers))]
        [Produces("application/json", "application/xml")]
        public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Min(Math.Max(pageSize, 1), 20);
            var pageList = userRepository.GetPage(pageNumber, pageSize);
            
            var paginationHeader = new
            {
                previousPageLink = pageList.HasPrevious ? 
                    GetUriByRouteValues(pageList.CurrentPage - 1, pageList.PageSize) : null,
                nextPageLink = pageList.HasNext ? 
                    GetUriByRouteValues(pageList.CurrentPage + 1, pageList.PageSize) : null,
                totalCount = pageList.TotalCount,
                pageSize = pageList.PageSize,
                currentPage = pageList.CurrentPage,
                totalPages = pageList.TotalPages,
            };
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
            
            var users = mapper.Map<IEnumerable<UserDto>>(pageList);
            return Ok(users);
        }

        private string GetUriByRouteValues(int pageNumber, int pageSize)
        {
            return linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new {pageNumber, pageSize});
        }

        [HttpOptions]
        public IActionResult Options()
        {
            Response.Headers.Add("Allow", "POST, GET, OPTIONS");
            return Ok();
        }
    }
}