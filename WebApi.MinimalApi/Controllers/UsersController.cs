using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NUnit.Framework.Constraints;
using Swashbuckle.AspNetCore.Annotations;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private IUserRepository userRepository;
    private IMapper mapper;
    private LinkGenerator linkGenerator;

    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        this.userRepository = userRepository;
        this.linkGenerator = linkGenerator;
        this.mapper = mapper;
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
        {
            return NotFound();
        }
        
        if (HttpMethods.IsHead(Request.Method))
        {
            Response.ContentType = "application/json; charset=utf-8";
            return Ok();
        }
        
        return Ok(mapper.Map<UserDto>(user));
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
    public IActionResult CreateUser([FromBody] PersonDTO user)
    {
        if (user == null)
        {
            return BadRequest();
        }        

        if (string.IsNullOrEmpty(user.Login) || !user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("Login", "Invalid login format");
            return UnprocessableEntity(ModelState);
        }
        
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }
        
        var userEntity = mapper.Map<UserEntity>(user);
        var insertedUser = userRepository.Insert(userEntity);

        return CreatedAtRoute(
        nameof(GetUserById),
         new { userId = insertedUser.Id },
         insertedUser.Id);
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
    public IActionResult UpdateUser([FromRoute] string userId, [FromBody] UpdateDto? user)
    {
        if (!Guid.TryParse(userId, out var userGuid) || user == null)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var userEntity = new UserEntity(userGuid)
        {
            Login = user.Login,
            FirstName = user.FirstName,
            LastName = user.LastName
        };

        try
        {
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
        catch (InvalidOperationException)
        {
            return NoContent();
        }
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
    public IActionResult PatchUser([FromRoute] string userId, [FromBody] JsonPatchDocument<UpdateDto> patchDoc)
    {

        if (patchDoc == null)
        {
            return BadRequest();
        }
        if (!Guid.TryParse(userId, out var userGuid) || patchDoc == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var user =  userRepository.FindById(userGuid);
        if (user == null)
        {
            return NotFound();
        }
   
        var userDto = mapper.Map<UpdateDto>(user);
        patchDoc.ApplyTo(userDto, ModelState);
        
        if (!TryValidateModel(userDto))
        { 
            return UnprocessableEntity(ModelState);
        }
        
        try
        {
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
        catch (InvalidOperationException)
        {
            return NoContent();
        }
    }

    /// <summary>
    /// Удалить пользователя
    /// </summary>
    /// <param name="userId">Идентификатор пользователя</param>
    [HttpDelete("{userId}")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(204, "Пользователь удален")]
    [SwaggerResponse(404, "Пользователь не найден")]
    public IActionResult DeleteUser([FromRoute] string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return NotFound();
        }

        var userEntity = userRepository.FindById(userGuid);
        if (userEntity == null)
        {
            return NotFound();
        }

        userRepository.Delete(userGuid);

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
        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = pageSize < 1 ? 1 : pageSize > 20 ? 20 : pageSize;
        
        var userEntities = userRepository.GetPage(pageNumber, pageSize);
        var users = mapper.Map<IEnumerable<UserDto>>(userEntities);

        var totalCount = userEntities.TotalCount;
        var totalPages = (int) Math.Ceiling(totalCount / (double)pageSize);
        var previousPage = pageNumber > 1 
            ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber - 1, pageSize}) 
            : null;
        var nextPageLink = pageNumber < totalPages
            ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber + 1, pageSize })
            : null;
        
        var paginationHeader = new
        {
            previousPageLink = previousPage,
            nextPageLink = nextPageLink,
            totalCount = totalCount,
            pageSize = pageSize,
            currentPage = pageNumber,
            totalPages = totalPages,
        };
        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        
        return Ok(users);
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
}