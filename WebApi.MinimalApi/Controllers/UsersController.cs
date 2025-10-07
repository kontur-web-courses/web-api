using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json", "application/xml")]
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
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(200, "OK", typeof(UserDto))]
    [SwaggerResponse(404, "Пользователь не найден")]
    [HttpGet("{userId:guid}", Name = nameof(GetUserById))]
    [HttpHead("{userId:guid}")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
        {
            return NotFound();
        }

        if (HttpMethods.IsHead(Request.Method))
        {
            return Content(string.Empty, "application/json; charset=utf-8");
        }

        return mapper.Map<UserDto>(user);
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
    public IActionResult CreateUser([FromBody] CreateUserDto? user)
    {
        if (user is null)
        {
            return BadRequest();
        }

        CheckLogin(user.Login);

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var createdUserEntity = mapper.Map<UserEntity>(user);
        var insertedUserEntity = userRepository.Insert(createdUserEntity);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = insertedUserEntity.Id },
            insertedUserEntity.Id);
    }

    /// <summary>
    /// Удалить пользователя
    /// </summary>
    /// <param name="userId">Идентификатор пользователя</param>
    [HttpDelete("{userId:guid}")]
    [Produces("application/json", "application/xml")]
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
    
    /// <summary>
    /// Обновить пользователя
    /// </summary>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="dto">Обновленные данные пользователя</param>
    [HttpPut("{userId}")]
    [Consumes("application/json")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(201, "Пользователь создан")]
    [SwaggerResponse(204, "Пользователь обновлен")]
    [SwaggerResponse(400, "Некорректные входные данные")]
    [SwaggerResponse(422, "Ошибка при проверке")]
    public ActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdateUserDto? dto)
    {
        if (userId == Guid.Empty || dto is null)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var user = mapper.Map(dto, new UserEntity(userId));

        userRepository.UpdateOrInsert(user, out var isInserted);

        if (isInserted)
        {
            return CreatedAtRoute(
                nameof(GetUserById),
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
    public ActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserDto>? patchDoc)
    {
        if (userId == Guid.Empty)
        {
            return NotFound();
        }

        if (patchDoc is null)
        {
            return BadRequest();
        }

        var updateUserDto = new UpdateUserDto();
        patchDoc.ApplyTo(updateUserDto, ModelState);
        TryValidateModel(updateUserDto);
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var existedUser = userRepository.FindById(userId);
        if (existedUser is null)
        {
            return NotFound();
        }

        var user = mapper.Map(updateUserDto, new UserEntity(userId));

        userRepository.Update(user);

        return NoContent();
    }

    /// <summary>
    /// Опции по запросам о пользователях
    /// </summary>
    [HttpOptions]
    [SwaggerResponse(200, "OK")]
    public IActionResult GetUsersOptions()
    {
        Response.Headers.Append("Allow", "POST, GET, OPTIONS");
        return Ok();
    }

    
    /// <summary>
    /// Получить пользователей
    /// </summary>
    /// <param name="dto">Номер страницы, по умолчанию 1</param>
    /// <response code="200">OK</response>
    [Produces("application/json", "application/xml")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
    [HttpGet(Name = nameof(GetAllUsers))]
    public IActionResult GetAllUsers(GetUsersDto dto)
    {
        var usersPage = userRepository.GetPage(dto.pageNumber, dto.pageSize);
        var users = mapper.Map<IEnumerable<UserDto>>(usersPage);

        var paginationHeader = new
        {
            previousPageLink = usersPage.HasPrevious
                ? linkGenerator
                    .GetUriByRouteValues(HttpContext, "GetAllUsers",
                        new { pageNumber = dto.pageNumber - 1, pageSize = dto.pageSize })
                : null,
            nextPageLink = usersPage.HasNext
                ? linkGenerator
                    .GetUriByRouteValues(HttpContext, "GetAllUsers",
                        new { pageNumber = dto.pageNumber + 1, pageSize = dto.pageSize })
                : null,
            totalCount = usersPage.TotalCount,
            pageSize = usersPage.PageSize,
            currentPage = usersPage.CurrentPage,
            totalPages = usersPage.TotalPages,
        };
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

        return Ok(users);
    }
    
    private void CheckLogin(string? login)
    {
        if (!string.IsNullOrEmpty(login) && !login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("Login", "Login can contain only letters and digits");
        }
    }
}