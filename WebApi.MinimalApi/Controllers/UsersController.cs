using System.Net.Mime;
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
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;
    private readonly LinkGenerator linkGenerator;
    private ISwaggerDescriptionsForUsersController swaggerDescriptionsForUsersControllerImplementation;

    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
        this.linkGenerator = linkGenerator;
    }

    [HttpGet("{userId:guid}", Name = nameof(GetUserById))]
    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(200, "OK", typeof(UserDto))]
    [SwaggerResponse(404, "Пользователь не найден")]
    public ActionResult<UserDto> GetUserById(Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
            return NotFound();

        return Ok(mapper.Map<UserDto>(user));
    }

    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(201, "Пользователь создан")]
    [SwaggerResponse(400, "Некорректные входные данные")]
    [SwaggerResponse(422, "Ошибка при проверке")]
    public IActionResult CreateUser([FromBody] UserToCreateDto user)
    {
        if (user is null)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }
        
        var userEntity = userRepository.Insert(mapper.Map<UserEntity>(user));
        
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = userEntity.Id },
            userEntity.Id);
    }

    [HttpPut("{userId}")]
    [Consumes("application/json")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(201, "Пользователь создан")]
    [SwaggerResponse(204, "Пользователь обновлен")]
    [SwaggerResponse(400, "Некорректные входные данные")]
    [SwaggerResponse(422, "Ошибка при проверке")]
    public IActionResult UpdateUser(UserToUpdateDto? userToUpdateDto, string userId)
    {
        if (!Guid.TryParse(userId, out var userIdGuid) || userToUpdateDto is null)
        {
            return BadRequest();
        }
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var userEntity = mapper.Map(userToUpdateDto, new UserEntity(userIdGuid));
        
        userRepository.UpdateOrInsert(userEntity, out var isInserted);

        return isInserted 
            ? CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id)
            : NoContent();
    }

    [HttpPatch("{userId:guid}")]
    [Consumes("application/json-patch+json")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(204, "Пользователь обновлен")]
    [SwaggerResponse(400, "Некорректные входные данные")]
    [SwaggerResponse(404, "Пользователь не найден")]
    [SwaggerResponse(422, "Ошибка при проверке")]
    public IActionResult PartiallyUpdateUser([FromBody] JsonPatchDocument<UserToUpdateDto> patchDoc, Guid userId)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }
        
        var user = userRepository.FindById(userId);

        if (user is null)
        {
            return NotFound();
        }
        
        var userToUpdateDto = mapper.Map<UserToUpdateDto>(user);
        
        
        patchDoc.ApplyTo(userToUpdateDto, ModelState);
        TryValidateModel(userToUpdateDto);
        
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }
        
        var updatedUserEntity = mapper.Map(userToUpdateDto, new UserEntity(userId));
        userRepository.Update(updatedUserEntity);
        
        return NoContent();
    }

    [HttpDelete("{userId:guid}")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(204, "Пользователь удален")]
    [SwaggerResponse(404, "Пользователь не найден")]
    public IActionResult DeleteUser(Guid userId)
    {
        if (userRepository.FindById(userId) is null)
        {
            return NotFound();
        }
        
        userRepository.Delete(userId);
        
        return NoContent();
    }

    [HttpHead("{userId:guid}")]
    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(200, "OK", typeof(UserDto))]
    [SwaggerResponse(404, "Пользователь не найден")]
    public IActionResult Head(Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
            return NotFound();
        
        HttpContext.Response.ContentType = "application/json; charset=utf-8";
        return Ok();
    }

    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
    public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 20) pageSize = 20;
        
        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);

        var totalCount = pageList.TotalCount;
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var paginationHeader = new
        {
            previousPageLink = pageNumber > 1 ? linkGenerator
                    .GetUriByRouteValues(
                        HttpContext,
                        "GetUsers",
                        new
                        {
                            pageNumber = pageNumber - 1,
                            pageSize
                        }) 
                : null,
            nextPageLink = linkGenerator.GetUriByRouteValues(HttpContext, "GetUsers", new { pageNumber = pageNumber + 1, pageSize }),
            totalCount = totalCount,
            pageSize = pageSize,
            currentPage = pageNumber,
            totalPages = totalPages
        };

        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        return Ok(users);
    }

    [HttpOptions]
    [SwaggerResponse(200, "OK")]
    public IActionResult Options()
    {
        Response.Headers.Append("Allow", "POST, GET, OPTIONS");

        return Ok();
    }
}