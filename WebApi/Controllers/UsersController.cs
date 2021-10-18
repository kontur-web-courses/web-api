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
        private IUserRepository _userRepository;
        private IMapper _userMapper;
        private LinkGenerator _linkGenerator;

        public UsersController(IUserRepository userRepository, IMapper userMapper, LinkGenerator linkGenerator)
        {
            _linkGenerator = linkGenerator;
            _userRepository = userRepository;
            _userMapper = userMapper;
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
            var user = new UserDto();
            var userEntity = _userRepository.FindById(userId);
            if (userEntity == null)
                return NotFound();
            return Ok(_userMapper.Map<UserDto>(userEntity));
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
        public IActionResult CreateUser([FromBody] UserCreationDto user)
        {
            if (user == null)
                return BadRequest();
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            if (user.Login.Where(c => !char.IsLetterOrDigit(c)).Any())
            {
                ModelState.AddModelError("Login", "Requires both letters and digits only");
                return UnprocessableEntity(ModelState);
            }   
            var userEntity = _userMapper.Map<UserEntity>(user);
            userEntity = _userRepository.Insert(userEntity);
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
        public IActionResult UpdateUser([FromBody] UserUpdateDto modifiedUser, [FromRoute] Guid userId)
        {
            if (modifiedUser == null || userId == Guid.Empty)
                return BadRequest();
            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }
            var user = _userMapper.Map(modifiedUser, _userRepository.FindById(userId) ?? new UserEntity(userId));
            _userRepository.UpdateOrInsert(user, out var isInserted);
            if (isInserted)
            {
                return CreatedAtRoute(nameof(GetUserById),
                    new { userId = user.Id },
                    user.Id);
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
        public IActionResult PatchUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserUpdateDto> patchDoc)
        {
            if (patchDoc == null)
                return BadRequest();
            var user = _userRepository.FindById(userId);
            if (user == null)
                return NotFound();
            var userUpdateDto = _userMapper.Map<UserEntity, UserUpdateDto>(user);
            patchDoc.ApplyTo(userUpdateDto, ModelState);
            TryValidateModel(userUpdateDto);
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            _userRepository.Update(_userMapper.Map(userUpdateDto, user));
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
            if (userId == Guid.Empty)
                return NotFound();
            var user = _userRepository.FindById(userId);
            if (user == null)
                return NotFound();
            _userRepository.Delete(userId);
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
        public ActionResult<IEnumerable<UserDto>> GetUsers([FromQuery] int pageNumber=1, [FromQuery] int pageSize=10)//что будет, если в параметрах будут строки?
        {
            if (pageNumber < 1)
                pageNumber = 1;
            if (pageSize < 1)
                pageSize = 1;
            if (pageSize > 20)
                pageSize = 20;
            var pageList = _userRepository.GetPage(pageNumber, pageSize);
            var users = _userMapper.Map<IEnumerable<UserDto>>(pageList);
            var prevPage = !pageList.HasPrevious ? null
                : _linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new
            {
                pageNumber = pageNumber - 1,
                pageSize = pageSize
            });

            var nextPage = !pageList.HasNext ? null
                : _linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new
            {
                pageNumber = pageNumber + 1,
                pageSize = pageSize
            });

            var paginationHeader = new
            {
                previousPageLink = prevPage,
                nextPageLink = nextPage,
                totalCount = pageList.TotalCount,
                pageSize = pageList.PageSize,
                currentPage = pageList.CurrentPage,
                totalPages = pageList.TotalPages
            };
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
            return Ok(users);
        }

        /// <summary>
        /// Опции по запросам о пользователях
        /// </summary>
        [HttpOptions]
        [SwaggerResponse(200, "OK")]
        public IActionResult UsersOptions()
        {
            Response.Headers.Add("Allow", "GET,POST,OPTIONS");
            return Ok();
        }
    }
}