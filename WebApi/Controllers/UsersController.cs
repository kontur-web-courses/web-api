using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
            if (user is null)
            {
                return NotFound();
            }

            var userDto = mapper.Map<UserDto>(user);
            return Ok(userDto);
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
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }
            // if (String.IsNullOrEmpty(user.Login) || user.Login.Any(c => !Char.IsLetterOrDigit(c)))
            if (user.Login.Any(c => !Char.IsLetterOrDigit(c)))
            {
                ModelState.AddModelError("login", "Некорректный логин");
                return UnprocessableEntity(ModelState);
            }
            
            
            var userEntity = mapper.Map<UserEntity>(user);
            var createdUserEntity = userRepository.Insert(userEntity);
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
        public ActionResult<UserEntity> UpdateUser([FromRoute] string userId, [FromBody] UserUpdateDto user)
        {
            if (!Guid.TryParse(userId, out Guid guid) || user is null)
            {
                return BadRequest();
            }
            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            UserEntity newUser = mapper.Map(user, new UserEntity(guid));
            
            
            userRepository.UpdateOrInsert(newUser, out bool inserted);

            if (inserted)
            {
                return CreatedAtRoute(
                    nameof(GetUserById),
                    new { userId = newUser.Id },
                    newUser.Id);
            }
            else
            {
                return NoContent();
            }

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
        public IActionResult PatchUser([FromRoute] Guid userId,
            [FromBody] JsonPatchDocument<UserUpdateDto> PatchDoc)
        {
            if (userId.Equals(Guid.Empty))
            {
                return NotFound();
            }

            if (PatchDoc is null)
            {
                return BadRequest();
            }

            UserEntity user = userRepository.FindById(userId);
            if (user is null)
            {
                return NotFound();
            }

            UserUpdateDto updateDto = mapper.Map<UserUpdateDto>(user);
            PatchDoc.ApplyTo(updateDto, ModelState);
            UserEntity patchedUser = mapper.Map(updateDto, new UserEntity(userId));
            Regex regex = new Regex("^[0-9\\p{L}]*$");
            if (String.IsNullOrEmpty(patchedUser.Login) ||
                !regex.IsMatch(patchedUser.Login))
            {
                ModelState.AddModelError("login", "Login must be not empty and should contain only letters or digits");
            }

            if (String.IsNullOrEmpty(patchedUser.FirstName))
            {
                ModelState.AddModelError("firstName", "FirstName must not be empty");
            }
            
            if (String.IsNullOrEmpty(patchedUser.LastName))
            {
                ModelState.AddModelError("lastName", "LastName must not be empty");
            }
            
            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }
            
            userRepository.Update(patchedUser);

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
            if (userId.Equals(Guid.Empty) || userRepository.FindById(userId) is null)
            {
                return NotFound();
            }
            
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
        public IActionResult GetUsers(
            [FromQuery(Name = "pageNumber")] int pageNumber = 1,
            [FromQuery(Name = "pageSize")] int pageSize = 10)
        {
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Max(pageSize, 1);
            pageSize = Math.Min(pageSize, 20);
            
            var pageList = userRepository.GetPage(pageNumber, pageSize);
            var users = mapper.Map<IEnumerable<UserDto>>(pageList);
            string previousePageLinkIfExists = null;
            string nextPageLinkIfExists = null;
            if (pageList.HasPrevious)
            {
                previousePageLinkIfExists = linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new
                {
                    pageNumber = pageNumber - 1,
                    pageSize
                });
            }
            if (pageList.HasNext)
            {
                nextPageLinkIfExists = linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new
                {
                    pageNumber = pageNumber + 1,
                    pageSize
                });
            }
            var paginationHeader = new
            {
                previousPageLink = previousePageLinkIfExists,
                nextPageLink = nextPageLinkIfExists,
                totalCount = pageList.TotalCount,
                pageSize = pageList.PageSize,
                currentPage = pageList.CurrentPage,
                totalPages = pageList.TotalPages,
            };
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
            
            return Ok(users);
        }

        /// <summary>
        /// Опции по запросам о пользователях
        /// </summary>
        [HttpOptions]
        [SwaggerResponse(200, "OK")]
        public IActionResult OptionsUsers()
        {
            string[] allowedMethods = new[]
            {
                "GET", "POST", "OPTIONS"
            };
            Response.Headers.Add("Allow", String.Join(", ", allowedMethods));

            return Ok();
        }
    }
}