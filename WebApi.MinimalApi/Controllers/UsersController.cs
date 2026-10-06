using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
using WebApi.MinimalApi.Models.Requests;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
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

        var response = mapper.Map<UserEntity, UserDto>(user);

        if (HttpMethods.IsHead(Request.Method))
        {
            Response.ContentType = "application/json; charset=utf-8";
            return StatusCode(200);
        }

        return Ok(response);
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
    public IActionResult CreateUser([FromBody] CreateUserRequest user)
    {
        if (user == null)
            return BadRequest();

        if (!(user.Login == null))
        {
            if (user.Login.Any(letter => !char.IsLetterOrDigit(letter)))
            {
                ModelState.AddModelError("login", "Login should contain only letters or digits");
            }
        }
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        var userEntity = mapper.Map<CreateUserRequest, UserEntity>(user);
        var result = userRepository.Insert(userEntity);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = result.Id },
             result.Id);
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
    public IActionResult UpsertUser(
        string userId, 
        [FromBody] UpsertUserRequest user)
    {
        if (user == null || !Guid.TryParse(userId, out Guid userGuid))
            return BadRequest();
        
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = new UserEntity(userGuid);
        mapper.Map(user, userEntity);

        bool result;
        userRepository.UpdateOrInsert(userEntity, out result);

        if (result)
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userId },
                userId);
        
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
    public IActionResult PartiallyUpdateUser(
        string userId,
        [FromBody] JsonPatchDocument<UpsertUserRequest> patchDoc)
    {
        if (patchDoc is null)
            return BadRequest();

        if (!Guid.TryParse(userId, out Guid userGuid))
            return NotFound();

        var userEntity = userRepository.FindById(userGuid);

        if (userEntity == null)
            return NotFound();

        var userDto = mapper.Map<UpsertUserRequest>(userEntity);

        patchDoc.ApplyTo(userDto, ModelState);
        
        TryValidateModel(userDto);

        if (!ModelState.IsValid ||
            string.IsNullOrWhiteSpace(userDto.Login) ||
            userDto.Login.Any(c => !char.IsLetterOrDigit(c)) ||
            string.IsNullOrWhiteSpace(userDto.FirstName) ||
            string.IsNullOrWhiteSpace(userDto.LastName))
        {
            return UnprocessableEntity(ModelState);
        }

        mapper.Map(userDto, userEntity);
        userRepository.Update(userEntity);

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
    public IActionResult DeleteUser(string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return NotFound();

        var user = userRepository.FindById(userGuid);
        if (user is null)
            return NotFound();
        
        userRepository.Delete(userGuid);
        
        return NoContent();
    }

    /// <summary>
    /// Получить пользователей
    /// </summary>
    /// <param name="pageNumber">Номер страницы, по умолчанию 1</param>
    /// <param name="pageSize">Размер страницы, по умолчанию 20</param>
    /// <response code="200">OK</response>
    [HttpGet("/api/users", Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
    public ActionResult<UserDto> GetUsers ([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        pageSize = Math.Clamp(pageSize, 1, 20);
        pageNumber = Math.Max(1, pageNumber);
        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);
        var paginationHeader = new
        {
            previousPageLink = pageList.CurrentPage == 1 ? null : linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageList.CurrentPage - 1, pageSize}),
            nextPageLink = pageList.CurrentPage == pageList.TotalPages ? null : linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageList.CurrentPage + 1, pageSize}),
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
    [HttpOptions("/api/users", Name = nameof(OptionsUsers))]
    [SwaggerResponse(200, "OK")]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> OptionsUsers()
    {
        Response.Headers.Add("Allow", "GET, POST, OPTIONS");
        return StatusCode(200);
    }
}