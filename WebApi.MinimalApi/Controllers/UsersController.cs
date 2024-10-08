using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Swashbuckle.Swagger.Annotations;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private readonly IMapper mapper;
    private readonly IUserRepository userRepository;
    private readonly LinkGenerator LinkGenerator;

    public UsersController(IUserRepository userRepository,
                           IMapper mapper,
                           LinkGenerator linkGenerator)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
        LinkGenerator = linkGenerator;
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
        if (HttpMethods.IsHead(HttpContext.Request.Method))
            Response.Body = Stream.Null;
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
    public IActionResult CreateUser([FromBody] UserToCreateDto? user)
    {
        if (user == null)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var userEntity = mapper.Map<UserEntity>(user);
        userEntity = userRepository.Insert(userEntity);

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
    public IActionResult UpdateUser([FromRoute] Guid? userId, [FromBody] UserToUpdateDto? user)
    {
        if (user == null || ModelState.IsInvalid(nameof(userId)))
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        userId ??= Guid.NewGuid();
        var mappedUser = mapper.Map(user, new UserEntity(userId.Value));
        userRepository.UpdateOrInsert(mappedUser, out var inserted);
        return inserted
            ? CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userId.Value },
                mappedUser.Id)
            : NoContent();
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
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserToUpdateDto>? patchDocument)
    {
        if (patchDocument == null)
            return BadRequest();
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        var userToUpdate = mapper.Map<UserToUpdateDto>(user);
        patchDocument.ApplyTo(userToUpdate, ModelState);
        if (!TryValidateModel(userToUpdate))
            return UnprocessableEntity(ModelState);
        mapper.Map(userToUpdate, user);
        userRepository.Update(user);
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
        if (!ModelState.IsValid)
            return NotFound();
        var user = userRepository.FindById(userId);
        if (user == null)
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
    public ActionResult<IEnumerable<UserDto>> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 20);
        var users = userRepository.GetPage(pageNumber, pageSize);
        var previousPageLink = users.CurrentPage == 1 ? null : LinkGenerator
            .GetUriByRouteValues(HttpContext,
                                 nameof(GetUsers),
                                 new
                                 {
                                     pageNumber = users.CurrentPage - 1,
                                     pageSize = users.PageSize
                                 });
        var nextPageLink = users.CurrentPage >= 20 ? null : LinkGenerator
            .GetUriByRouteValues(HttpContext,
                                 nameof(GetUsers),
                                 new
                                 {
                                     pageNumber = users.CurrentPage + 1,
                                     pageSize = users.PageSize
                                 });
        var paginationHeader = new
        {
            previousPageLink,
            nextPageLink,
            totalCount = users.TotalCount,
            pageSize = users.PageSize,
            currentPage = users.CurrentPage,
            totalPages = users.TotalPages
        };
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        return Ok(users);
    }

    /// <summary>
    /// Опции по запросам о пользователях
    /// </summary>
    [HttpOptions]
    [SwaggerResponse(200, "OK")]
    public IActionResult UsersOptions()
    {
        var allowedMethods = new[] { HttpMethods.Post, HttpMethods.Get, HttpMethods.Options };
        Response.Headers.AppendList(HeaderNames.Allow, allowedMethods);
        return Ok();
    }
}