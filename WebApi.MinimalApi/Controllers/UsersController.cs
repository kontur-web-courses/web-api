using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json.Linq;
using System.Web.Http.ModelBinding;
using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;
using Swashbuckle.Swagger.Annotations;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[Produces("application/json", "application/xml")]
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
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(200, "OK", typeof(UserDto))]
    [SwaggerResponse(404, "Пользователь не найден")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user != null)
        {
            return mapper.Map<UserDto>(user);
        }

        return NotFound();
    }

    [HttpHead("{userId}")]
    public ActionResult<UserDto> FindUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user != null)
            return Content("", "application/json; charset=utf-8");
        return NotFound();
    }

    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(201, "Пользователь создан")]
    [SwaggerResponse(400, "Некорректные входные данные")]
    [SwaggerResponse(422, "Ошибка при проверке")]
    public IActionResult CreateUser([FromBody] CreateUserDto user)
    {
        if (user is not null)
        {
            if (user.Login == null || user.Login.Any(x => !char.IsLetterOrDigit(x)))
            {
                ModelState.AddModelError("Login", "Недопустимые символы");
            }

            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            var userEntity = userRepository.Insert(mapper.Map<UserEntity>(user));

            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id);
        }
        else
        {
            return BadRequest();
        }
    }

    [HttpPut("{userId}")]
    [Consumes("application/json")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(201, "Пользователь создан")]
    [SwaggerResponse(204, "Пользователь обновлен")]
    [SwaggerResponse(400, "Некорректные входные данные")]
    [SwaggerResponse(422, "Ошибка при проверке")]
    public IActionResult UpdateUser([FromRoute] string userId, [FromBody] UpdateUserDto user)
    {
        Guid userGuid;
        var isGuidValid = Guid.TryParse(userId, out userGuid);

        if (user is not null && isGuidValid)
        {
            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            user.Id = userGuid;

            bool isInserted;

            userRepository.UpdateOrInsert(mapper.Map<UserEntity>(user), out isInserted);

            if (isInserted)
            {
                return CreatedAtRoute(
                    nameof(GetUserById),
                    new { userId },
                    userId);
                
            }
            else
            {
                return NoContent();
            }
        }
        else
        {
            return BadRequest();
        }
    }

    [HttpPatch("{userId}")]
    [Consumes("application/json-patch+json")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(204, "Пользователь обновлен")]
    [SwaggerResponse(400, "Некорректные входные данные")]
    [SwaggerResponse(404, "Пользователь не найден")]
    [SwaggerResponse(422, "Ошибка при проверке")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc)
    {
        if (patchDoc is not null)
        {
            var user = userRepository.FindById(userId);

            if (user is null)
            {
                return NotFound();
            }

            var userDto = mapper.Map<UpdateUserDto>(user);
            patchDoc.ApplyTo(userDto, ModelState);

            TryValidateModel(userDto);

            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            userRepository.Update(mapper.Map<UserEntity>(userDto));

            return NoContent();
        }
        else
        {
            return BadRequest();
        }
    }

    [HttpDelete("{userId}")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(204, "Пользователь удален")]
    [SwaggerResponse(404, "Пользователь не найден")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user != null)
        {
            userRepository.Delete(userId);
            return NoContent();
        }

        return NotFound();
    }

    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
    public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var paginationHeader = new
        {
            previousPageLink = pageList.HasPrevious
                ? linkGenerator.GetUriByRouteValues(HttpContext, "GetUsers", new { pageNumber = pageNumber - 1, pageSize })
                : null,
            nextPageLink = pageList.HasNext
                ? linkGenerator.GetUriByRouteValues(HttpContext, "GetUsers", new { pageNumber = pageNumber + 1, pageSize })
                : null,
            totalCount = pageList.TotalCount,
            pageSize = pageSize < 1 ? 1 : (pageSize > 20 ? 20 : pageSize),
            currentPage = pageNumber < 1 ? 1 : pageNumber,
            totalPages = pageList.TotalPages,
        };

        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);

        return Ok(users);
    }

    [HttpOptions]
    [SwaggerResponse(200, "OK")]
    public IActionResult OptionsUsers()
    { 
        Response.Headers.Add("Allow", "POST, GET, OPTIONS");
        return Ok();
    }
}