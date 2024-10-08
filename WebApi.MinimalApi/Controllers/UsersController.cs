using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

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
    /// <response code="200">OK</response>
    /// <response code="404">Пользователь не найден</response>
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(200, "OK", typeof(UserDto))]
    [SwaggerResponse(404, "Пользователь не найден")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user == null)
        {
            return NotFound();
        }

        if (HttpContext.Request.Method == "HEAD")
        {
            Response.ContentType = "application/json; charset=utf-8";
            return Ok();
        }

        return Ok(_mapper.Map<UserDto>(user));
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
    /// <response code="201">Пользователь создан</response>
    /// <response code="400">Некорректные входные данные</response>
    /// <response code="422">Ошибка при проверке</response>
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(201, "Пользователь создан")]
    [SwaggerResponse(400, "Некорректные входные данные")]
    [SwaggerResponse(422, "Ошибка при проверке")]
    public IActionResult CreateUser([FromBody] UserCreationDto? user)
    {
        if (user == null)
        {
            return BadRequest();
        }
        
        if (string.IsNullOrWhiteSpace(user.Login))
        {
            ModelState.AddModelError("Login", "Login is required");
        }
        else
        {
            if (user.Login.Any(c => !char.IsLetterOrDigit(c)))
            {
                ModelState.AddModelError("Login", "Login must consist only of letters and digits");
            }
        }
        
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }
        var createdUserEntity = _mapper.Map<UserEntity>(user);
        var tmp = _userRepository.Insert(createdUserEntity);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = tmp.Id },
            tmp.Id);
    }
    
    /// <summary>
    /// Обновить пользователя
    /// </summary>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="user">Обновленные данные пользователя</param>
    /// <response code="201">Пользователь создан</response>
    /// <response code="204">Пользователь обновлен</response>
    /// <response code="400">Некорректные входные данные</response>
    /// <response code="422">Ошибка при проверке</response>
    [HttpPut("{userId}")]
    [Consumes("application/json")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(201, "Пользователь создан")]
    [SwaggerResponse(204, "Пользователь обновлен")]
    [SwaggerResponse(400, "Некорректные входные данные")]
    [SwaggerResponse(422, "Ошибка при проверке")]
    public IActionResult UpdateUser([FromBody] UpdateDto? user, [FromRoute] Guid userId)
    {
        if (user == null || userId == Guid.Empty)
        {
            return BadRequest();
        }
        
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }
        
        var updatedUserEntity = new UserEntity(userId);
        _mapper.Map(user, updatedUserEntity);
        _userRepository.UpdateOrInsert(updatedUserEntity, out var isInserted);
        if (!isInserted)
        {
            return NoContent();
        }
        
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = updatedUserEntity.Id },
            updatedUserEntity.Id);
    }

    /// <summary>
    /// Частично обновить пользователя
    /// </summary>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="patchDoc">JSON Patch для пользователя</param>
    /// <response code="204">Пользователь обновлен</response>
    /// <response code="400">Некорректные входные данные</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="422">Ошибка при проверке</response>
    [HttpPatch("{userId}")]
    [Consumes("application/json-patch+json")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(204, "Пользователь обновлен")]
    [SwaggerResponse(400, "Некорректные входные данные")]
    [SwaggerResponse(404, "Пользователь не найден")]
    [SwaggerResponse(422, "Ошибка при проверке")]
    public IActionResult PartiallyUpdateUser([FromBody] JsonPatchDocument<UpdateDto>? patchDoc, [FromRoute] Guid userId)
    {
        if (patchDoc == null)
        {
            return BadRequest();
        }
        
        var user  = _userRepository.FindById(userId);
        if (user == null || userId == Guid.Empty)
        {
            return NotFound();
        }
        
        var updateDto = _mapper.Map<UpdateDto>(user);
        
        patchDoc.ApplyTo(updateDto, ModelState);
        TryValidateModel(updateDto);
        
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var updatedUserEntity = new UserEntity(userId);
        _mapper.Map(updateDto, updatedUserEntity);
        _userRepository.Update(updatedUserEntity);
        
        return NoContent();
    }
    
    /// <summary>
    /// Удалить пользователя
    /// </summary>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <response code="204">Пользователь обновлен</response>
    /// <response code="404">Пользователь не найден</response>
    [HttpDelete("{userId}")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(204, "Пользователь удален")]
    [SwaggerResponse(404, "Пользователь не найден")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user == null)
        {
            return NotFound();
        }

        _userRepository.Delete(userId);

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
    public IActionResult GetUsers([FromServices] LinkGenerator linkGenerator, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        pageNumber = (pageNumber < 1) ? 1 : pageNumber;
        pageSize = (pageSize < 1) ? 1 : (pageSize > 20) ? 20 : pageSize;

        var pageList = _userRepository.GetPage(pageNumber, pageSize);
        
        var users = _mapper.Map<IEnumerable<UserDto>>(pageList);
        
        var totalCount = pageList.TotalCount;
        var totalPages = pageList.TotalPages;
        var hasPreviousPage = pageNumber > 1;
        var hasNextPage = pageNumber < totalPages;
        
        var previousPageLink = hasPreviousPage
            ? linkGenerator.GetUriByRouteValues(HttpContext, null, new { pageNumber = pageNumber - 1, pageSize })
            : null;

        var nextPageLink = hasNextPage
            ? linkGenerator.GetUriByRouteValues(HttpContext, null, new { pageNumber = pageNumber + 1, pageSize })
            : null;
        
        var paginationHeader = new
        {
            previousPageLink,
            nextPageLink,
            totalCount,
            pageSize,
            currentPage = pageNumber,
            totalPages
        };
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        
        return Ok(users);
    }
    
    /// <summary>
    /// Опции по запросам о пользователях
    /// </summary>
    /// <response code="200">OK</response>
    [HttpOptions]
    [SwaggerResponse(200, "OK")]
    public IActionResult GetUsersOptions()
    {
        Response.Headers.Append("Allow", "GET, POST, OPTIONS");
        return Ok();
    }
}