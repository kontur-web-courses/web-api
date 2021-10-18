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

        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
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
            if (user == null)
                return NotFound();
            
            return Ok(mapper.Map<UserDto>(user));
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
            if (userRepository.FindById(userId) == null)
                return NotFound();
            userRepository.Delete(userId);
            return NoContent();
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

        /// <summary>
        /// Опции по запросам о пользователях
        /// </summary>
        [HttpOptions]
        [SwaggerResponse(200, "OK")]
        public IActionResult Options()
        {
            Response.Headers.Add("Allow", "POST, GET, OPTIONS");
            return Ok();
        }
    }
}