using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        private IUserRepository userRepository;
        private IMapper mapper;
        private LinkGenerator linkGenerator;
        
        public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
        {
            this.userRepository = userRepository;
            this.mapper = mapper;
            this.linkGenerator = linkGenerator;
        }

        /// <summary>
        /// Получить пользователя
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [HttpHead("{userId}")]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(200, "OK", typeof(UserDto))]
        [SwaggerResponse(404, "Пользователь не найден")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId);
            if (user is null)
                return NotFound();
            var userDto = mapper.Map<UserDto>(user);
            return Ok(userDto);
        }

        /// <summary>
        /// Получить пользователей
        /// </summary>
        /// <param name="pageNumber">Номер страницы, по умолчанию 1</param>
        /// <param name="pageSize">Размер страницы, по умолчанию 20</param>
        /// <response code="200">OK</response>
        [HttpGet(Name = nameof(GetUsers))]
        [Produces("application/json", "application/xml")]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
        public ActionResult<UserDto> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Max(1, Math.Min(20, pageSize));
            
            var pageList = userRepository.GetPage(pageNumber, pageSize);
            var prevPage = pageList.HasPrevious
                ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), 
                    new {pageNumber = pageNumber - 1, pageSize})
                : null;
            var nextPage = pageList.HasNext 
                ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), 
                    new {pageNumber = pageNumber + 1, pageSize})
                : null;
            
            var paginationHeader = new
            {
                previousPageLink = prevPage,
                nextPageLink = nextPage,
                totalCount = pageList.TotalCount,
                pageSize = pageSize,
                currentPage = pageNumber,
                totalPages = pageList.TotalPages,
            };
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
            
            var users = mapper.Map<IEnumerable<UserDto>>(pageList);
            return Ok(users);
        }

        
        /// <summary>
        /// Создать пользователя
        /// </summary>
        /// <remarks>
        /// Пример запроса:
        ///
        ///     POST /api/users
        ///     {
        ///        "login": "johndoe375",
        ///        "firstName": "John",
        ///        "lastName": "Doe"
        ///     }
        ///
        /// </remarks>
        /// <param name="user">Данные для создания пользователя</param>
        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(201, "Пользователь создан")]
        [SwaggerResponse(400, "Некорректные входные данные")]
        [SwaggerResponse(422, "Ошибка при проверке")]
        public IActionResult CreateUser([FromBody] UserToCreateDto userDto)
        {
            if (userDto is null)
                return BadRequest();

            if (string.IsNullOrEmpty(userDto.Login) || !userDto.Login.All(char.IsLetterOrDigit))
                ModelState.AddModelError("Login", "Login should contain only letters or digits");
            
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var user = mapper.Map<UserEntity>(userDto);
            var userEntity = userRepository.Insert(user);
            
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id);
        }

        /// <summary>
        /// Обновить пользователя
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <param name="user">Обновленные данные пользователя</param>
        [HttpPut("{userId}")]
        [Consumes("application/json")]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(201, "Пользователь создан")]
        [SwaggerResponse(204, "Пользователь обновлен")]
        [SwaggerResponse(400, "Некорректные входные данные")]
        [SwaggerResponse(422, "Ошибка при проверке")]
        public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UserToUpdateDto userDto)
        {
            if (userDto is null || userId == Guid.Empty)
                return BadRequest();
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var user = new UserEntity(userId);
            mapper.Map(userDto, user);
            
            userRepository.UpdateOrInsert(user, out var isInserted);
            
            if (!isInserted)
                return NoContent();
            
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = user.Id },
                user.Id);
        }
        
        /// <summary>
        /// Частично обновить пользователя
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <param name="patchDoc">JSON Patch для пользователя</param>
        [HttpPatch("{userId}")]
        [Consumes("application/json-patch+json")]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(204, "Пользователь обновлен")]
        [SwaggerResponse(400, "Некорректные входные данные")]
        [SwaggerResponse(404, "Пользователь не найден")]
        [SwaggerResponse(422, "Ошибка при проверке")]
        public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, 
            [FromBody] JsonPatchDocument<UserToUpdateDto> patchDoc)
        {
            if (patchDoc is null)
                return BadRequest();
            
            var user = userRepository.FindById(userId);
            
            if (user is null)
                return NotFound();

            var userDto = new UserToUpdateDto();
            patchDoc.ApplyTo(userDto, ModelState);
            TryValidateModel(userDto);
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            
            mapper.Map(userDto, user);
            userRepository.Update(user);
            
            return NoContent();
        }
        
        /// <summary>
        /// Удалить пользователя
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        [HttpDelete("{userId}")]
        [Produces("application/json", "application/xml")]
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
        
        /// <summary>
        /// Опции по запросам о пользователях
        /// </summary>
        [HttpOptions]
        [SwaggerResponse(200, "OK")]
        public IActionResult GetUsersOptions()
        {
            Response.Headers.Add("Allow", "GET, POST, OPTIONS");
            return Ok();
        } 
    }
}