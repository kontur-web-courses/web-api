using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using Microsoft.Net.Http.Headers;
using Swashbuckle.AspNetCore.Annotations;

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
        this.userRepository = userRepository;
        this.linkGenerator = linkGenerator;
        this.mapper = mapper;
    }

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
        var userDto = mapper.Map<UserDto>(user);
        return Ok(userDto);
    }

    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(201, "Пользователь создан")]
    [SwaggerResponse(400, "Некорректные входные данные")]
    [SwaggerResponse(422, "Ошибка при проверке")]
    public IActionResult CreateUser([FromBody] UserDtoToCreate? user)
    {
        if (user == null)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var userEntity = userRepository.Insert(mapper.Map<UserEntity>(user));
        return CreatedAtRoute(nameof(GetUserById), new { userId = userEntity.Id }, userEntity.Id);
    }
    
    public static bool IsInvalid(ModelStateDictionary modelState, string key)
    {
        return modelState.GetValidationState(key) == ModelValidationState.Invalid;
    }

    [HttpPut("{userId}")]
    [Consumes("application/json")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(201, "Пользователь создан")]
    [SwaggerResponse(204, "Пользователь обновлен")]
    [SwaggerResponse(400, "Некорректные входные данные")]
    [SwaggerResponse(422, "Ошибка при проверке")]
    public IActionResult UpdateUser([FromRoute] Guid? userId, [FromBody] UserDtoToUpdate? user)
    {
        if (user == null || IsInvalid(ModelState, nameof(userId)))
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        userId ??= Guid.NewGuid();
        var entity = mapper.Map(user, new UserEntity(userId.Value));
        userRepository.UpdateOrInsert(entity, out var inserted);
        if (inserted)
            return CreatedAtRoute(nameof(GetUserById), new { userId = userId.Value }, entity.Id);
        return NoContent();
    }

    [HttpPatch("{userId}")]
    [Consumes("application/json-patch+json")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(204, "Пользователь обновлен")]
    [SwaggerResponse(400, "Некорректные входные данные")]
    [SwaggerResponse(404, "Пользователь не найден")]
    [SwaggerResponse(422, "Ошибка при проверке")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserDtoToUpdate>? patchDocument)
    {
        if (patchDocument == null)
            return BadRequest();
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var toUpdate = mapper.Map<UserDtoToUpdate>(user);
        patchDocument.ApplyTo(toUpdate, ModelState);
        if (!TryValidateModel(toUpdate))
            return UnprocessableEntity(ModelState);
        var entity = mapper.Map(toUpdate, user);
        userRepository.Update(entity);
        return NoContent();
    }

    [HttpDelete("{userId}")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(204, "Пользователь удален")]
    [SwaggerResponse(404, "Пользователь не найден")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();
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
    public ActionResult<IEnumerable<UserDto>> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1)
            pageNumber = 1;
        if (pageSize < 1)
            pageSize = 1;
        if (pageSize > 20)
            pageSize = 20;
        var users = userRepository.GetPage(pageNumber, pageSize);
        var previousPageLink = users.CurrentPage > 1
            ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new
                {
                    pageNumber = users.CurrentPage - 1,
                    pageSize = users.PageSize
                }) : null;
        var nextPageLink = users.CurrentPage < 20
            ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new
                {
                    pageNumber = users.CurrentPage + 1,
                    pageSize = users.PageSize
                }) : null;
        var header = new
        {
            previousPageLink,
            nextPageLink,
            totalCount = users.TotalCount,
            pageSize = users.PageSize,
            currentPage = users.CurrentPage,
            totalPages = users.TotalPages
        };
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(header));
        return Ok(users);
    }

    [HttpOptions]
    [SwaggerResponse(200, "OK")]
    public IActionResult Options()
    {
        var allowedMethods = new[] { HttpMethods.Post, HttpMethods.Get, HttpMethods.Options };
        Response.Headers.AppendList(HeaderNames.Allow, allowedMethods);
        return Ok();
    }
}