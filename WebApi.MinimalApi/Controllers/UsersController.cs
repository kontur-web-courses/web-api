using System.Text.Json;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
using WebApi.MinimalApi.Models.Requests;

namespace WebApi.MinimalApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json", "application/xml")]
public class UsersController : Controller
{
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;
    private readonly LinkGenerator linkGenerator;

    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
        this.linkGenerator = linkGenerator;
    }

    /// <summary>
    /// Returns user by its id
    /// </summary>
    [HttpHead("{userId}")]
    [HttpGet("{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        if (HttpMethods.IsHead(Request.Method))
            Response.Body = Stream.Null;
        var user = userRepository.FindById(userId);
        return user is not null ? Ok(mapper.Map<UserDto>(user)) : NotFound();
    }

    /// <summary>
    /// Creates user and returns its id
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public IActionResult CreateUser([FromBody] CreateUserRequest request)
    {
        if (request is null)
            return BadRequest();
        if (!request.Login?.All(char.IsAsciiLetterOrDigit) is true)
            ModelState.AddModelError("Login", "Login should contain only digits and letters");
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var entity = mapper.Map<UserEntity>(request);
        var result = userRepository.Insert(entity);
        return CreatedAtAction(nameof(GetUserById), new { userId = result.Id }, result.Id);
    }

    /// <summary>
    /// Changes user by id or creates if doesn't exist
    /// </summary>
    [HttpPut("{userId}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public IActionResult UpsertUser([FromRoute] Guid userId, [FromBody] UpdateUserRequest request)
    {
        if (request is null || userId == Guid.Empty)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var entity = mapper.Map<UserEntity>(request);
        entity.Id = userId;
        userRepository.UpdateOrInsert(entity, out var inserted);
        return inserted
            ? CreatedAtAction(nameof(GetUserById), new { userId }, userId)
            : NoContent();
    }

    /// <summary>
    /// Updates user data
    /// </summary>
    [HttpPatch("{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserRequest> patchDoc)
    {
        if (patchDoc is null)
            return BadRequest();
        var user = userRepository.FindById(userId);
        if (user is null)
            return NotFound();
        var update = mapper.Map<UpdateUserRequest>(user);
        patchDoc.ApplyTo(update, ModelState);
        TryValidateModel(update);
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var entity = mapper.Map<UserEntity>(update);
        entity.Id = userId;
        userRepository.Update(entity);
        return NoContent();
    }

    /// <summary>
    /// Deletes user
    /// </summary>
    [HttpDelete("{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (userRepository.FindById(userId) is null)
            return NotFound();
        userRepository.Delete(userId);
        return NoContent();
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet(Name = nameof(GetUsers))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<UserDto>> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        pageSize = Math.Clamp(pageSize, 1, 20);
        pageNumber = Math.Max(pageNumber, 1);
        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var paginationInfo = new
        {
            previousPageLink = pageList.HasPrevious
                ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber - 1, pageSize })
                : null,
            nextPageLink = pageList.HasNext
                ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber + 1, pageSize })
                : null,
            pageList.TotalCount,
            pageList.PageSize,
            pageList.CurrentPage,
            pageList.TotalPages,
        };
        Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationInfo));
        return Ok(mapper.Map<IEnumerable<UserDto>>(pageList));
    }

    [HttpOptions]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Options()
    {
        var allowedMethods = new[] { HttpMethods.Get, HttpMethods.Options, HttpMethods.Post };
        Response.Headers.Add("Allow", string.Join(", ", allowedMethods));
        return Ok();
    }
}