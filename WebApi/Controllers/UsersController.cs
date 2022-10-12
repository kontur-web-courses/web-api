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
        private readonly IUserRepository userRepository;
        private readonly IMapper mapper;
        private readonly LinkGenerator linkGenerator;

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
        [HttpHead("{userId}")]
        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(200, "OK", typeof(UserDto))]
        [SwaggerResponse(404, "Пользователь не найден")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId);
            if (user is null) return NotFound();
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
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(201, "Пользователь создан")]
        [SwaggerResponse(400, "Некорректные входные данные")]
        [SwaggerResponse(422, "Ошибка при проверке")]
        public IActionResult CreateUser([FromBody] UserToCreateDto user)
        {
            if (user is null) return BadRequest();
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

            if (!user.Login.All(char.IsLetterOrDigit))
            {
                ModelState.AddModelError("Login", "Login should contain only letters or digits");
                return UnprocessableEntity(ModelState);
            }

            var createdUserEntity = userRepository.Insert(mapper.Map<UserEntity>(user));
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id
            );
        }

        /// <summary>
        /// Обновить пользователя
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <param name="user">Обновленные данные пользователя</param>
        [HttpPut("{userId}")]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(201, "Пользователь создан")]
        [SwaggerResponse(204, "Пользователь обновлен")]
        [SwaggerResponse(400, "Некорректные входные данные")]
        [SwaggerResponse(422, "Ошибка при проверке")]
        public IActionResult UpdateUser([FromRoute] Guid? userId, [FromBody] UserToUpdateDto user)
        {
            if (user is null || userId is null) return BadRequest();
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

            user.Id = userId.Value;
            userRepository.UpdateOrInsert(mapper.Map<UserEntity>(user), out var isInserted);
            return !isInserted
                ? NoContent()
                : CreatedAtRoute(
                      nameof(GetUserById),
                      new { userId = userId },
                      userId
                  );
        }

        /// <summary>
        /// Частично обновить пользователя
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <param name="patchDoc">JSON Patch для пользователя</param>
        [HttpPatch("{userId}")]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(204, "Пользователь обновлен")]
        [SwaggerResponse(400, "Некорректные входные данные")]
        [SwaggerResponse(404, "Пользователь не найден")]
        [SwaggerResponse(422, "Ошибка при проверке")]
        public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserToUpdateDto> patchDoc)
        {
            if (patchDoc is null) return BadRequest();
            var user = userRepository.FindById(userId);
            if (user is null) return NotFound();

            var updateDto = mapper.Map<UserToUpdateDto>(user);
            patchDoc.ApplyTo(updateDto, ModelState);
            TryValidateModel(updateDto);
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);
            var newUser = mapper.Map<UserEntity>(updateDto);

            userRepository.Update(newUser);
            return NoContent();
        }

        /// <summary>
        /// Удалить пользователя
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        [HttpDelete("{userId}")]
        [SwaggerResponse(204, "Пользователь удален")]
        [SwaggerResponse(404, "Пользователь не найден")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId);
            if (user is null) return NotFound();
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
        public ActionResult<IEnumerable<UserDto>> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Min(Math.Max(1, pageSize), 20);

            var pageList = userRepository.GetPage(pageNumber, pageSize);

            string prevPageLink = pageList.HasPrevious
                ? linkGenerator.GetUriByRouteValues(HttpContext,
                    nameof(GetUsers), new { pageNumber = pageNumber - 1, pageSize })
                : null;
            string nextPageLink = pageList.HasNext
                ? linkGenerator.GetUriByRouteValues(HttpContext,
                    nameof(GetUsers), new { pageNumber = pageNumber + 1, pageSize })
                : null;
            var paginationHeader = new
            {
                PreviousPageLink = prevPageLink,
                NextPageLink = nextPageLink,
                TotalCount = pageList.TotalCount,
                PageSize = pageList.PageSize,
                CurrentPage = pageList.CurrentPage,
                TotalPages = pageList.TotalCount,
            };
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

            return Ok(mapper.Map<IEnumerable<UserDto>>(pageList));
        }

        /// <summary>
        /// Опции по запросам о пользователях
        /// </summary>
        [HttpOptions]
        [SwaggerResponse(200, "OK")]
        public IActionResult GetUsersOptions()
        {
            Response.Headers.Add("Allow", new[] { "POST", "GET", "OPTIONS" });
            return Ok();
        }
    }
}