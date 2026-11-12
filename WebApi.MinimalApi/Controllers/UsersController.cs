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
    private IUserRepository _userRepository;
    private IMapper _mapper;
    private LinkGenerator _linkGenerator;

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
    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(200, "OK", typeof(UserDto))]
    [SwaggerResponse(404, "Пользователь не найден")]
    public IActionResult GetUserById([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user == null)
            return NotFound();

        if (HttpMethods.IsHead(Request.Method))
        {
            var result = new OkResult();
            Response.ContentType = "application/json; charset=utf-8";
            return result;
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
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(201, "Пользователь создан")]
    [SwaggerResponse(400, "Некорректные входные данные")]
    [SwaggerResponse(422, "Ошибка при проверке")]
    public IActionResult CreateUser([FromBody] UserCreateDto user)
    {
        if (user is null)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        if (!user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("Login", "Should contain only letters or digits");
            return UnprocessableEntity(ModelState);
        }

        var createdUser = _mapper.Map<UserEntity>(user);
        var createdUserEntity = _userRepository.Insert(createdUser);
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
    public IActionResult UpdateUser([FromBody] UserUpdateDto userInfo, [FromRoute] Guid userId)
    {
        if (userInfo is null)
        {
            return BadRequest();
        }

        if (userInfo.Login is null || userInfo.FirstName is null || userInfo.LastName is null)
        {
            return UnprocessableEntity(ModelState);
        }

        userInfo.Id = userId;
        var isInserted = false;
        var user = _mapper.Map<UserEntity>(userInfo);
        try
        {
            _userRepository.UpdateOrInsert(user, out isInserted);
        }
        catch (Exception ex)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

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
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserUpdateDto> patchDoc)
    {
        if (patchDoc is null)
        {
            return BadRequest();
        }
        
        var userInfo = new  UserUpdateDto();
        patchDoc.ApplyTo(userInfo, ModelState);
        
        userInfo.Id = userId;


        if (_userRepository.FindById(userId) is null)
        {
            return NotFound();
        }

        
        
        TryValidateModel(userInfo);
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }
        
        _userRepository.Update(_mapper.Map<UserEntity>(userInfo));
        
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
        if (_userRepository.FindById(userId) is null)
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
    [HttpGet(Name = "GetUsers")]
    [Produces("application/json", "application/xml")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
    public IActionResult GetUsers([FromQuery] string? pageNumber, [FromQuery] string? pageSize)
    {
        var pageSizeInt = 10;
        var pageNumberInt = 1;
        
        if (pageSize is not null)
        {
            var pageSizeParsed = int.Parse(pageSize);
            if (pageSizeParsed < 1) pageSizeInt = 1;
            else if (pageSizeParsed > 20) pageSizeInt = 20;
            else  pageSizeInt = pageSizeParsed;
        }

        if (pageNumber is not null)
        {
            var pageNumberParsed = int.Parse(pageNumber);
            if (pageNumberParsed < 1) pageNumberInt = 1;
            else  pageNumberInt = pageNumberParsed;
        }
        
        var pageList = _userRepository.GetPage(pageNumberInt, pageSizeInt);
        var users = _mapper.Map<IEnumerable<UserDto>>(pageList);
        var previousPageLink = pageNumberInt != 1? _linkGenerator.GetUriByRouteValues(HttpContext, "GetUsers", new {pageNumber = pageNumberInt - 1, pageSize = pageSizeInt}) : null;
        var nextPageLink = _linkGenerator.GetUriByRouteValues(HttpContext, "GetUsers", new {pageNumber = pageNumberInt + 1, pageSize = pageSizeInt});
        var paginationHeader = new
        {
            previousPageLink = previousPageLink,
            nextPageLink = nextPageLink,
            totalCount = pageList.TotalCount,
            pageSize = pageSizeInt,
            currentPage = pageNumberInt,
            totalPages = pageList.TotalPages,
        };
        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        return Ok(users);
    }
    
    /// <summary>
    /// Опции по запросам о пользователях
    /// </summary>
    [HttpOptions]
    [Produces("application/json", "application/xml")]
    public IActionResult Options()
    {
        Response.Headers.Add("Allow", "POST, GET, OPTIONS");
        return Ok();
    }
}