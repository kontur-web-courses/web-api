using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

/// <summary>
/// Users controller
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Produces("application/json", "application/xml")]
public class UsersController : Controller
{
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;
    private readonly LinkGenerator linkGenerator;
    
    /// <summary>
    /// </summary>
    /// <param name="userRepository"></param>
    /// <param name="mapper"></param>
    /// <param name="linkGenerator"></param>
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
    [HttpHead("{userId}")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [SwaggerResponse(200, "OK", typeof(UserDto))]
    [SwaggerResponse(404, "Пользователь не найден")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user != null)
        {
            var userDto = mapper.Map<UserDto>(user);
            return HttpMethods.IsHead(Request.Method)
                ? new ContentResult { StatusCode = 200, ContentType = "application/json; charset=utf-8"}
                : Ok(userDto);
        }
        
        return NotFound();
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
    [SwaggerResponse(201, "Пользователь создан")]
    [SwaggerResponse(400, "Некорректные входные данные")]
    [SwaggerResponse(422, "Ошибка при проверке")]
    public IActionResult CreateUser([FromBody] UserForCreateDto? user)
    {
        if (user == null)
        {
            return BadRequest();
        }
        
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        if (!user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("Login", "Login should contain only letters or digits");
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
    [SwaggerResponse(201, "Пользователь создан")]
    [SwaggerResponse(204, "Пользователь обновлен")]
    [SwaggerResponse(400, "Некорректные входные данные")]
    [SwaggerResponse(422, "Ошибка при проверке")]
    public IActionResult PutUser([FromRoute] Guid userId, [FromBody] UserForPutDto? user)
    {
        if (user == null || userId == Guid.Empty)
        {
            return BadRequest();
        }
        
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = new UserEntity(userId);
        mapper.Map(user, userEntity);
        userRepository.UpdateOrInsert(userEntity, out var isInserted);

        if (isInserted)
        {
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id);
        }
        
        return NoContent();
    }

    /// <summary>
    /// Частично обновить пользователя
    /// </summary>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="patchDoc">JSON Patch для пользователя</param>
    [HttpPatch("{userId}")]
    [SwaggerResponse(204, "Пользователь обновлен")]
    [SwaggerResponse(400, "Некорректные входные данные")]
    [SwaggerResponse(404, "Пользователь не найден")]
    [SwaggerResponse(422, "Ошибка при проверке")]
    public IActionResult PartiallyUpdateUser(
        [FromRoute] Guid userId,
        [FromBody] JsonPatchDocument<UserToUpdateDto>? patchDoc)
    {
        if (userId == Guid.Empty)
        {
            return NotFound();
        }
        
        var user = userRepository.FindById(userId);
        
        if (patchDoc == null)
            return BadRequest();
        
        if (user == null) 
            return NotFound();
        
        var userDto = new UserToUpdateDto();
        mapper.Map(user, userDto);
        patchDoc.ApplyTo(userDto, ModelState);
        if (!ModelState.IsValid || !TryValidateModel(userDto))
            return UnprocessableEntity(ModelState);
        
        
        var userEntity = new UserEntity(userId);
        mapper.Map(user, userEntity);
        userRepository.Update(userEntity);

        return NoContent();
    }

    /// <summary>
    /// Удалить пользователя
    /// </summary>
    /// <param name="userId">Идентификатор пользователя</param>
    [HttpDelete("{userId}")]
    [SwaggerResponse(204, "Пользователь удален")]
    [SwaggerResponse(404, "Пользователь не найден")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (userId == Guid.Empty || userRepository.FindById(userId) == null)
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
    [HttpGet(Name = nameof(GetAllUsers))]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
    public IActionResult GetAllUsers([FromQuery] PaginationParameters paginationParameters)
    {
        var (pageNumber, pageSize) = (paginationParameters.PageNumber, paginationParameters.PageSize);

        if (!ModelState.IsValid)
        {
            (pageNumber, pageSize) = (Math.Max(1, pageNumber), Math.Max(1, Math.Min(20, pageSize)));
        }
        
        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);

        var generateLink = new Func<int, string?>(pn =>
            linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetAllUsers), new { pageNumber = pn, pageSize}));
        
        var paginationHeader = new
        {
            previousPageLink = pageList.HasPrevious ? generateLink(pageNumber - 1) : null,
            nextPageLink = pageList.HasNext ? generateLink(pageNumber + 1) : null,
            totalCount = pageList.TotalCount,
            pageSize = pageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages,
        };
        
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

        return Ok(users);
    }

    /// <summary>
    /// Опции по запросам о пользователях
    /// </summary>
    [HttpOptions]
    [SwaggerResponse(200, "OK")]
    public IActionResult Options()
    {
        Response.Headers.Append("Allow", "POST,GET,OPTIONS");
        
        return Ok();
    }
}