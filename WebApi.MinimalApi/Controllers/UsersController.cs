using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
using WebApi.MinimalApi.Samples;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
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
    public ActionResult<UserDto>? GetUserById([FromRoute] Guid userId)
    {
        var userEntity = _userRepository.FindById(userId);
        if (userEntity is null) return NotFound();
        var userDto = _mapper.Map<UserDto>(userEntity);
        if (!HttpMethods.IsHead(Request.Method)) return Ok(userDto);
        Response.Headers.ContentType = "application/json; charset=utf-8";
        return Ok();
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
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Max(1, pageSize);
        pageSize = Math.Min(20, pageSize);
        var users = _userRepository.GetPage(pageNumber, pageSize);
        var paginationHeader = new
        {
            previousPageLink = users.HasPrevious
                ? Url.Link(nameof(GetUsers), new
                {
                    pageNumber = pageNumber - 1,
                    pageSize = pageSize
                })
                : null,
            nextPageLink = users.HasNext
                ? Url.Link(nameof(GetUsers), new
                {
                    pageNumber = pageNumber + 1,
                    pageSize = pageSize
                })
                : null,
            totalCount = users.TotalCount,
            pageSize = pageSize,
            currentPage = users.CurrentPage,
            totalPages = users.TotalPages,
        };
        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
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
    public IActionResult CreateUser([FromBody] UserCreateDto user)
    {
        if (user is null)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        if (!user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("Login", "Login should contain only letters or digits");
            return UnprocessableEntity(ModelState);
        }

        var userEntity = _mapper.Map<UserEntity>(user);
        userEntity = _userRepository.Insert(userEntity);
        var guid = userEntity.Id;
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = guid }, guid);
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
    public IActionResult UpsertUser(Guid userId, [FromBody] UserUpdateDto user)
    {
        if (userId == Guid.Empty || user is null)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var userEntity = _userRepository.FindById(userId);
        userEntity ??= new UserEntity(userId);
        userEntity = _mapper.Map(user, userEntity);
        _userRepository.UpdateOrInsert(userEntity, out var isInserted);
        if (isInserted)
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId }, new { guid = userId });

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
    public IActionResult DeleteUSer(Guid userId)
    {
        if (userId == Guid.Empty || _userRepository.FindById(userId) is null)
            return NotFound();
        _userRepository.Delete(userId);
        return NoContent();
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
    public IActionResult PartiallyUpdateUser(Guid userId, [FromBody] JsonPatchDocument<UserUpdateDto> patchDoc)
    {
        if (patchDoc is null) return BadRequest();
        var user = _userRepository.FindById(userId);
        if (user is null) return NotFound();
        var updateDto = _mapper.Map<UserUpdateDto>(user);
        patchDoc.ApplyTo(updateDto, ModelState);
        if (!TryValidateModel(updateDto))
            return UnprocessableEntity(ModelState);
        user = _mapper.Map(updateDto, user);
        _userRepository.UpdateOrInsert(user, out var inserted);
        return NoContent();
    }
}