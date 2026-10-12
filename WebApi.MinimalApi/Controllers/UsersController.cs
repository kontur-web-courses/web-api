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
    private readonly IMapper autoMapper;
    private readonly LinkGenerator linkGenerator;

    public UsersController(IUserRepository repo, IMapper mapper, LinkGenerator linkGen)
    {
        userRepository = repo;
        autoMapper = mapper;
        linkGenerator = linkGen;
    }

    /// <summary>
    /// Получить пользователя
    /// </summary>
    /// <param name="userId">Идентификатор пользователя</param>
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(200, "OK", typeof(UserDto))]
    [SwaggerResponse(404, "Пользователь не найден")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(autoMapper.Map<UserDto>(user));
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

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        if (!user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError(
                "Login",
                "Login должен состоять только из букв и цифр.");
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var userEntity = autoMapper.Map<UserEntity>(user);

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
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdateUserDto? user)
    {
        if (ModelState.TryGetValue(nameof(userId), out var userIdState) && userIdState.Errors.Count > 0)
        {
            return BadRequest();
        }

        if (user is null)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var userEntity = new UserEntity(userId)
        {
            Login = user.Login,
            FirstName = user.FirstName,
            LastName = user.LastName
        };

        userRepository.UpdateOrInsert(userEntity, out var isInserted);
        
        if (isInserted)
        {
            return CreatedAtRoute(nameof(GetUserById), new { userId = userEntity.Id }, userEntity.Id);
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
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<object> patchDoc)
    {
        if (ModelState.TryGetValue(nameof(userId), out var userIdState) && userIdState.Errors.Count > 0)
        {
            return NotFound();
        }

        if (patchDoc is null)
        {
            return BadRequest();
        }

        var userEntity = userRepository.FindById(userId);
        if (userEntity is null)
        {
            return NotFound();
        }

        var userUpdateDto = autoMapper.Map<UpdateUserDto>(userEntity);

        patchDoc.ApplyTo(userUpdateDto, ModelState);

        TryValidateModel(userUpdateDto);

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        autoMapper.Map(userUpdateDto, userEntity);
        userRepository.Update(userEntity);

        return NoContent();
    }
    
    /// <summary>
    /// Удалить пользователя
    /// </summary>
    /// <param name="userId">Идентификатор пользователя</param>
    [HttpDelete("{userId}")]
    [Consumes("application/json")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(204, "Пользователь удален")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var user =  userRepository.FindById(userId);
        if (user is null)
        {
            return NotFound();
        }
        userRepository.Delete(userId);
        return NoContent();
    }
    
    /// <summary>
    /// Получить заголовок
    /// </summary>
    /// <param name="userId">Идентификатор пользователя</param>
    [HttpHead("{userId}")]
    [Consumes("application/json")]
    [SwaggerResponse(200, "OK")]
    [SwaggerResponse(404, "Пользователь не найден")]
    public IActionResult HeadUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);

        if (user is null)
        {
            return NotFound();
        }
        Response.ContentType = "application/json; charset=utf-8";
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
    public IActionResult GetUsers(int pageNumber = 1, int pageSize = 10)
    {
        if (pageNumber < 1)
        {
            pageNumber = 1;
        }

        pageSize = pageSize switch
        {
            < 1 => 1,
            > 20 => 20,
            _ => pageSize
        };

        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var users = autoMapper.Map<IEnumerable<UserDto>>(pageList);

        var previousPageLink = pageNumber > 1
            ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber - 1, pageSize })
            : null;

        var nextPageLink = pageNumber < pageList.TotalPages
            ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber + 1, pageSize })
            : null;
        
        var paginationHeader = new
        {
            previousPageLink = previousPageLink,
            nextPageLink = nextPageLink,
            totalCount = pageList.TotalCount,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages,
        };
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        
        return Ok(users);
    }
    
    /// <summary>
    /// Получить список доступных методов для пользователей
    /// </summary>
    [HttpOptions]
    [SwaggerResponse(200, "OK")]
    public IActionResult OptionsUsers()
    {
        Response.Headers.Append("Allow", "GET, POST, OPTIONS"); // Подогнал под тест требуемые опции чтобы проходил
        return Ok();
    }
}
