using System;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        public IUserRepository userRepository;
        public IMapper mapper;
        public LinkGenerator linkGenerator;

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
        [SwaggerResponse(200, "OK", typeof(UserDto))]
        [SwaggerResponse(404, "Пользователь не найден")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId);
            if (user == null)
                return NotFound();
            var u = new UserDto();
            u.FullName = $"{user.LastName} {user.FirstName}";
            u.CurrentGameId = user.CurrentGameId;
            u.Id = user.Id;
            u.Login = user.Login;
            u.GamesPlayed = user.GamesPlayed;
            return Ok(u);
        }

        [HttpPost]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(201, "Пользователь создан")]
        [SwaggerResponse(400, "Некорректные входные данные")]
        [SwaggerResponse(422, "Ошибка при проверке")]
        public IActionResult CreateUser([FromBody] DtoUserCreate user)
        {
            if (user == null)
                return BadRequest();
            if (user.Login == null || user.Login.Length == 0 
                || user.Login.Where(x => char.IsLetterOrDigit(x)).Count() != user.Login.Length)
                ModelState.AddModelError(nameof(user.Login), "Login should contain only letters or digits");
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            var userDto = mapper.Map<UserEntity>(user);
            var createdUser = userRepository.Insert(userDto);
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUser.Id },
                createdUser.Id);
        }

        [HttpPut("{userId}")]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(201, "Пользователь создан")]
        [SwaggerResponse(204, "Пользователь обновлен")]
        [SwaggerResponse(400, "Некорректные входные данные")]
        [SwaggerResponse(422, "Ошибка при проверке")]
        public IActionResult UpdateUser([FromBody] DtoUserUpdate user, [FromRoute] Guid userId)
        {
            if (user == null || Guid.Empty == userId)
                return BadRequest();
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            var userDto = mapper.Map(user, new UserEntity(userId));
            userRepository.UpdateOrInsert(userDto, out var inserted);
            if (!inserted)
                return NoContent();
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userDto.Id },
                userDto.Id);
        }

        [Produces("application/json", "application/xml")]
        [HttpPatch("{userId}")]
        [SwaggerResponse(204, "Пользователь обновлен")]
        [SwaggerResponse(400, "Некорректные входные данные")]
        [SwaggerResponse(404, "Пользователь не найден")]
        [SwaggerResponse(422, "Ошибка при проверке")]
        public IActionResult PartiallyUpdateUser([FromBody] JsonPatchDocument<DtoUserUpdate> document, [FromRoute] Guid userId)
        {
            if (document == null)
                return BadRequest();
            var user = userRepository.FindById(userId);
            if (user != null)
            {
                var userDto = mapper.Map<DtoUserUpdate>(user);
                document.ApplyTo(userDto, ModelState);
                TryValidateModel(userDto);
                if (!ModelState.IsValid)
                    return UnprocessableEntity(ModelState);
                mapper.Map(userDto, user);
                userRepository.Update(user);
                return NoContent();
            }
            return NotFound();
        }

        [Produces("application/json", "application/xml")]
        [HttpDelete("{userId}")]
        [SwaggerResponse(204, "Пользователь удален")]
        [SwaggerResponse(404, "Пользователь не найден")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            if (userRepository.FindById(userId) != null)
            {
                userRepository.Delete(userId);
                return NoContent();
            }
            return NotFound();
        }

        [HttpOptions]
        [SwaggerResponse(200, "OK")]
        public IActionResult GetUsersOptions()
        {
            Response.Headers.Add("Allow", new[] {"GET", "OPTIONS", "POST" });
            return Ok();
        }

        [HttpGet]
        [Produces("application/json", "application/xml")]
        public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1)
                pageNumber = 1;
            if (pageSize < 1)
                pageSize = 1;
            if (pageSize > 20)
                pageSize = 20;
            var pageList = userRepository.GetPage(pageNumber, pageSize);
            var users = mapper.Map<IEnumerable<UserDto>>(pageList);
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(new
            {
                previousPageLink = !pageList.HasPrevious
                    ? null
                    : linkGenerator.GetUriByAction(
                        HttpContext,
                        nameof(GetUsers),
                        values: new { pageNumber = pageList.CurrentPage - 1, pageSize = pageList.PageSize }),
                nextPageLink = !pageList.HasNext
                    ? null
                    : linkGenerator.GetUriByAction(
                        HttpContext,
                        nameof(GetUsers),
                        values: new { pageNumber = pageList.CurrentPage + 1, pageSize = pageList.PageSize }),
                totalCount = pageList.TotalCount,
                pageSize = pageList.PageSize,
                currentPage = pageList.CurrentPage,
                totalPages = pageList.TotalPages,
            }));
            return Ok(users);
        }
    }
}