using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace WebApi.MinimalApi.Controllers;

[Produces("application/json", "application/xml")]
[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly LinkGenerator _linkGenerator;

    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _linkGenerator = linkGenerator;
    }

    /// <summary>
    /// Получить пользователя
    /// </summary>
    /// <param name="userId">Идентификатор пользователя</param>
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [HttpHead("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(200, "OK", typeof(UserDto))]
    [SwaggerResponse(404, "Пользователь не найден")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user == null)
            return NotFound();

        var userDto = _mapper.Map<UserDto>(user);

        if (!HttpMethods.IsHead(Request.Method))
            return userDto;

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
        pageSize = Math.Clamp(pageSize, 1, 20);

        var pageList = _userRepository.GetPage(pageNumber, pageSize);
        var users = _mapper.Map<IEnumerable<UserDto>>(pageList);
        var paginationHeader = new
        {
            previousPageLink = pageList.HasPrevious
                ? _linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), values: new
                {
                    pageNumber = pageNumber - 1,
                    pageSize
                })
                : null,
            nextPageLink = pageList.HasNext
                ? _linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), values: new
                {
                    pageNumber = pageNumber + 1,
                    pageSize
                })
                : null,
            totalCount = pageList.TotalCount,
            pageSize = pageSize,
            currentPage = pageNumber,
            totalPages = (int)Math.Ceiling((double)pageList.TotalCount / pageSize),
        };

        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

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
    public IActionResult CreateUser([FromBody] CreateUserDto? user)
    {
        if (user is null)
            return BadRequest();

        if (string.IsNullOrEmpty(user.Login) || !user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("Login", "Invalid login format");
            return UnprocessableEntity(ModelState);
        }

        var entity = _mapper.Map<UserEntity>(user);
        var createdId = _userRepository.Insert(entity).Id;

        return CreatedAtRoute(nameof(GetUserById), new { userId = entity.Id }, createdId);
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
    public IActionResult UpdateUser([FromRoute] string userId, [FromBody] UpdateUserDto? user)
    {
        if (!Guid.TryParse(userId, out var userGuid) || user == null)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);


        var entity = new UserEntity(userGuid);
        _mapper.Map(user, entity);
        _userRepository.UpdateOrInsert(entity, out var inserted);

        if (inserted)
            return CreatedAtRoute(nameof(GetUserById), new { userId = userGuid }, userGuid);

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
    public IActionResult PartiallyUpdateUser(Guid userId, [FromBody] JsonPatchDocument<UpdateUserDto>? patchDoc)
    {
        if (patchDoc == null)
            return BadRequest();

        var userEntity = _userRepository.FindById(userId);
        if (userEntity == null)
            return NotFound();

        var updateUser = new UpdateUserDto()
        {
            Login = userEntity.Login,
            FirstName = userEntity.FirstName,
            LastName = userEntity.LastName
        };

        patchDoc.ApplyTo(updateUser, ModelState);
        
        TryValidateModel(updateUser);
        
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        userEntity.Login = updateUser.Login;
        userEntity.FirstName = updateUser.FirstName;
        userEntity.LastName = updateUser.LastName;
        _userRepository.Update(userEntity);
        return NoContent();
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
        if (_userRepository.FindById(userId) is null)
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
        Response.Headers.Append("Allow", "GET, POST, OPTIONS");
        return Ok();
    }
}