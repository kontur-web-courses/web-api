using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
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
        
        public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
        {
            this.userRepository = userRepository;
            this.mapper = mapper;
            this.linkGenerator = linkGenerator;
        }

        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [HttpHead("{userId}")]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(200, "OK", typeof(UserDto))]
        [SwaggerResponse(404, "Пользователь не найден")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var userEntity = userRepository.FindById(userId);
            if (userEntity is null)
                return NotFound();
            var userDto = mapper.Map<UserDto>(userEntity);
            
            return Ok(userDto);
        }

        [HttpPost]
        [SwaggerResponse(201, "Пользователь создан")]
        [SwaggerResponse(400, "Некорректные входные данные")]
        [SwaggerResponse(422, "Ошибка при проверке")]
        public IActionResult CreateUser([FromBody] UserCreationDto user)
        {
            if (user is null)
                return BadRequest();
            if (user.Login is null || user.Login.Equals(string.Empty) || !user.Login.All(char.IsLetterOrDigit))
                ModelState.AddModelError("Login", "Login should contain only letters or digits");
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var userEntity = mapper.Map<UserEntity>(user);
            var createdUserEntity = userRepository.Insert(userEntity);
            
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
        }

        [HttpPut("{userId}")]
        [SwaggerResponse(201, "Пользователь создан")]
        [SwaggerResponse(204, "Пользователь обновлен")]
        [SwaggerResponse(400, "Некорректные входные данные")]
        [SwaggerResponse(422, "Ошибка при проверке")]
        public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UserUpdateDto user)
        {
            if (userId.Equals(Guid.Empty) || user is null)
                return BadRequest();
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            
            var userEntity = mapper.Map(user, new UserEntity(userId));
            userRepository.UpdateOrInsert(userEntity, out var isInserted);
            
            if (!isInserted)
                return NoContent();
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id);
        }

        [HttpPatch("{userId}")]
        [SwaggerResponse(204, "Пользователь обновлен")]
        [SwaggerResponse(400, "Некорректные входные данные")]
        [SwaggerResponse(404, "Пользователь не найден")]
        [SwaggerResponse(422, "Ошибка при проверке")]
        public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserUpdateDto> patchDocument)
        {
            if (patchDocument is null)
                return BadRequest();
            
            var userEntity = userRepository.FindById(userId);
            if (userEntity is null)
                return NotFound();
            
            var userDto = mapper.Map<UserUpdateDto>(userEntity);
            patchDocument.ApplyTo(userDto, ModelState);
            TryValidateModel(userDto);
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            mapper.Map(userDto, userEntity);
            userRepository.Update(userEntity);
            
            return NoContent();
        }

        [HttpDelete("{userId}")]
        [SwaggerResponse(204, "Пользователь удален")]
        [SwaggerResponse(404, "Пользователь не найден")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId);
            if (user is null)
                return NotFound();
            userRepository.Delete(userId);
            return NoContent();
        }

        [HttpGet(Name = nameof(GetUsers))]
        [Produces("application/json", "application/xml")]
        public ActionResult<IEnumerable<UserDto>> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            pageSize = pageSize < 1 ? 1 : pageSize > 20 ? 20 : pageSize;
            var userEntitiesPage = userRepository.GetPage(pageNumber, pageSize);
            var users = mapper.Map<IEnumerable<UserDto>>(userEntitiesPage);

            var previousPageLink = userEntitiesPage.HasPrevious
                ? linkGenerator.GetUriByRouteValues(
                    HttpContext, nameof(GetUsers), new { pageNumber = userEntitiesPage.CurrentPage - 1, pageSize = userEntitiesPage.PageSize})
                : null;
            var nextPageLink = userEntitiesPage.HasNext
                ? linkGenerator.GetUriByRouteValues(
                    HttpContext, nameof(GetUsers), new { pageNumber = userEntitiesPage.CurrentPage + 1, pageSize = userEntitiesPage.PageSize})
                : null;
            
            var paginationHeader = new
            {
                previousPageLink,
                nextPageLink,
                userEntitiesPage.TotalCount,
                userEntitiesPage.PageSize,
                userEntitiesPage.CurrentPage,
                userEntitiesPage.TotalPages,
            };
            
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
            return Ok(users);
        }

        [HttpOptions]
        [SwaggerResponse(200, "OK")]
        public IActionResult GetOptions()
        {
            var methodNames = new[] { HttpMethod.Post.ToString(), HttpMethod.Get.ToString(), HttpMethod.Options.ToString() };
            Response.Headers.Add("Allow", new StringValues(methodNames));
            return Ok();
        }
    }
}