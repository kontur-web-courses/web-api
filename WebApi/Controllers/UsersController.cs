using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using NUnit.Framework;
using Swashbuckle.AspNetCore.Annotations;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        private IUserRepository userRepository;
        private IMapper mapper;
        private LinkGenerator linkGenerator;
        public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
        {
            this.userRepository = userRepository;
            this.mapper = mapper;
            this.linkGenerator = linkGenerator;
        }
        [HttpHead("{userId}")]
        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(200, "OK", typeof(UserDto))]
        [SwaggerResponse(404, "Пользователь не найден")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var src = userRepository.FindById(userId);
            if (src is null)
            {
                return NotFound();
            }
            var user = mapper.Map<UserDto>(src);
            return user;
        }

        [HttpPost]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(201, "Пользователь создан")]
        [SwaggerResponse(400, "Некорректные входные данные")]
        [SwaggerResponse(422, "Ошибка при проверке")]
        public IActionResult CreateUser([FromBody] CreateUserDto userDto)
        {
            if (userDto is null)
            {
                return BadRequest();
            }
            if (!IsLoginValid(userDto.Login))
            {
                ModelState.AddModelError("Login", "Login should contain only letters or digits");
                return UnprocessableEntity(ModelState);
            };
            var createdUserEntity = mapper.Map<UserEntity>(userDto);
            createdUserEntity = userRepository.Insert(createdUserEntity);
            var result = CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
            return result;
        }

        [HttpPut ("{userId}")]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(201, "Пользователь создан")]
        [SwaggerResponse(204, "Пользователь обновлен")]
        [SwaggerResponse(400, "Некорректные входные данные")]
        [SwaggerResponse(422, "Ошибка при проверке")]
        public IActionResult UpdateUser([FromBody] UpdateUserDto userDto, [FromRoute] Guid userId)
        {
            if (userDto == null || userId == Guid.Empty)
            {
                return BadRequest();
            }
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("Login", "Login should not be null");
                ModelState.AddModelError("FirstName", "FirstName should not be null");
                
                ModelState.AddModelError("LastName", "LastName should not be null");
                return UnprocessableEntity(ModelState);
            }
            var userEntity = new UserEntity(userId);
            mapper.Map(userDto, userEntity);
            bool alreadyCreated;
            userRepository.UpdateOrInsert(userEntity, out alreadyCreated);
            if (alreadyCreated)
                return CreatedAtRoute(
                    nameof(GetUserById),
                    new { userId = userEntity.Id },
                    userEntity.Id);
            return new NoContentResult();
        }

        [HttpPatch("{userId}")]
        [Consumes("application/json-patch+json")]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(204, "Пользователь обновлен")]
        [SwaggerResponse(400, "Некорректные входные данные")]
        [SwaggerResponse(404, "Пользователь не найден")]
        [SwaggerResponse(422, "Ошибка при проверке")]
        public IActionResult PatchUser([FromBody] JsonPatchDocument<PatchUserDto> patchDoc, [FromRoute] Guid userId)
        {
            var userEntity = userRepository.FindById(userId);
            var userDto = mapper.Map<PatchUserDto>(userEntity);
            if (patchDoc is null)
                return BadRequest();
            if (userEntity is null)
                return NotFound();
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("Login", "Login should not be null");
                ModelState.AddModelError("FirstName", "FirstName should not be null");
                
                ModelState.AddModelError("LastName", "LastName should not be null");
                return UnprocessableEntity(ModelState);
            }
            patchDoc.ApplyTo(userDto, ModelState);
            if (!TryValidateModel(userDto))
            {
                return UnprocessableEntity(ModelState);
            }
            var updatedUserEntity = mapper.Map<UserEntity>(userDto);
            bool alreadyCreated;
            userRepository.UpdateOrInsert(updatedUserEntity, out alreadyCreated);
            return new NoContentResult();
        }

        [HttpDelete("{userId}")]
        [SwaggerResponse(204, "Пользователь удален")]
        [SwaggerResponse(404, "Пользователь не найден")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            var userEntity = userRepository.FindById(userId);
            if (userEntity is null)
                return NotFound();
            userRepository.Delete(userId);
            return NoContent();
        }

        
        [HttpGet( Name = nameof(GetUsers))]
        [Produces("application/json", "application/xml")]
        
        [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
        public IActionResult GetUsers([FromQuery] int pageNumber=1,[FromQuery] int pageSize=10)
        {
            if (pageSize > 20) pageSize = 20;
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 1;
            var pageList = userRepository.GetPage(pageNumber, pageSize);
            var users = mapper.Map<IEnumerable<UserDto>>(pageList);
            var paginationHeader = new
            {
                previousPageLink = pageNumber == 1? null: linkGenerator
                    .GetUriByRouteValues(HttpContext, nameof(GetUsers), new {pageNumber=pageNumber-1, pageSize=pageSize}),
                nextPageLink = linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new {pageNumber=pageNumber+1, pageSize=pageSize}),
                totalCount = pageList.TotalCount,
                pageSize = pageList.PageSize,
                currentPage = pageList.CurrentPage,
                totalPages = pageList.TotalCount,
            };
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

            return Ok(users);

        }

        [HttpOptions]
        [SwaggerResponse(200, "OK")]
        public IActionResult Options()
        {
            Response.Headers.Add("Allow", "POST,GET,OPTIONS");
            return Ok();
        }

        private static bool IsLoginValid(string login)
        {
            return login != null && login.All(char.IsLetterOrDigit);
        }
    }

    public class CreateUserDto
    {
        [Required]
        public string Login { get; set; }
        [DefaultValue("John")]
        public string FirstName{ get; set; }
        [DefaultValue("Doe")]
        public string LastName{ get; set; }
    }

    public class UpdateUserDto
    {
        [Required]
        [RegularExpression("^[0-9\\p{L}]*$", ErrorMessage = "Login should contain only letters or digits")]
        public string Login { get; set; }
        [Required]
        public string FirstName{ get; set; }
        [Required]
        public string LastName{ get; set; }
    }

    public class PatchUserDto
    {
        [Required]
        [RegularExpression("^[0-9\\p{L}]*$", ErrorMessage = "Login should contain only letters or digits")]
        public string Login { get; set; }
        [Required]
        public string FirstName{ get; set; }
        [Required]
        public string LastName{ get; set; }
        public Guid Id { get; set; }
    }
}