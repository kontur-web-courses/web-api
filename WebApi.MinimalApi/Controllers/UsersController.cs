using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.Swagger.Annotations;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;
    private readonly LinkGenerator linkGenerator;

    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        this.mapper = mapper;
        this.userRepository = userRepository;
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
            return NotFound();

        if (HttpContext.Request.Method.Equals("HEAD", StringComparison.InvariantCulture))
        {
            Response.ContentType = "application/json; charset=utf-8";
            return Ok();
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
    public IActionResult CreateUser([FromBody] UserCreateDto? user)
    {
        if (user == null)
            return BadRequest();

        Validate(user);
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

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
    public IActionResult UpdateUser([FromBody] UserUpdateDto? user, [FromRoute] Guid userId)
    {
        if (user == null || userId == Guid.Empty)
            return BadRequest();

        Validate(user);
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = mapper.Map(user, new UserEntity(userId));
        userRepository.UpdateOrInsert(userEntity, out var isInserted);
        if (isInserted)
            return CreatedAtAction(
                actionName: nameof(GetUserById),
                routeValues: new { userId },
                value: userId);

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
    public IActionResult PartialUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserUpdateDto> patchDoc)
    {
        if (patchDoc == null)
            return BadRequest();

        var existingUser = userRepository.FindById(userId);
        if (existingUser == null || userId == Guid.Empty)
            return NotFound();

        var userToUpdate = mapper.Map<UserUpdateDto>(existingUser);
        patchDoc.ApplyTo(userToUpdate, ModelState);
        Validate(userToUpdate);
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var entity = mapper.Map<UserEntity>(userToUpdate);
        userRepository.Update(entity);

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
        if (userId == Guid.Empty)
            return BadRequest();
        if (userRepository.FindById(userId) is null)
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
    public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Max(pageSize, 1);
        pageSize = Math.Min(pageSize, 20);

        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var totalCount = userRepository.GetTotalCount();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var previousPageLink = pageNumber > 1
            ? linkGenerator.GetUriByRouteValues(
                HttpContext,
                routeName: "GetUsers",
                values: new { pageNumber = pageNumber - 1, pageSize })
            : null;

        var nextPageLink = linkGenerator.GetUriByRouteValues(HttpContext,
            routeName: "GetUsers",
            values: new { pageNumber = pageNumber + 1, pageSize });

        var paginationHeader = new
        {
            previousPageLink = previousPageLink,
            nextPageLink = nextPageLink,
            pageSize = pageSize,
            currentPage = pageNumber,
            totalCount = totalCount,
            totalPages = totalPages
        };
        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);
        return Ok(users);
    }

    /// <summary>
    /// Опции по запросам о пользователях
    /// </summary>
    [HttpOptions]
    public IActionResult GetOptions()
    {
        Response.Headers.Append("Allow", "GET, POST, OPTIONS");
        return Ok();
    }

    private void Validate(UserUpdateDto user)
    {
        if (string.IsNullOrEmpty(user.Login) || !user.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError("login", "Invalid login format");

        if (string.IsNullOrEmpty(user.FirstName))
            ModelState.AddModelError("firstName", "Invalid name format");

        if (string.IsNullOrEmpty(user.LastName))
            ModelState.AddModelError("lastName", "Invalid name format");
    }

    private void Validate(UserCreateDto user)
    {
        if (string.IsNullOrEmpty(user.Login) || !user.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError("login", "Invalid login format");
    }
}