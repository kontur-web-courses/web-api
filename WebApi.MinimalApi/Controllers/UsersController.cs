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

    [HttpHead("{userId}")]
    [HttpGet("{userId}")]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        if (HttpMethods.IsHead(Request.Method))
            Response.Body = Stream.Null;
        var user = userRepository.FindById(userId);
        return user is not null ? Ok(mapper.Map<UserDto>(user)) : NotFound();
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
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

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
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

    [HttpPatch("{userId}")]
    [Produces("application/json", "application/xml")]
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

    [HttpDelete("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (userRepository.FindById(userId) is null)
            return NotFound();
        userRepository.Delete(userId);
        return NoContent();
    }

    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    public ActionResult<IEnumerable<UserDto>> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        pageSize = Math.Clamp(pageSize,1, 20);
        if (pageNumber < 1) pageNumber = 1;
        var totalCount = userRepository.GetPage(1, int.MaxValue).Count;
        var paginationInfo = new
        {
            previousPageLink = pageNumber == 1 
                ? null
                : linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber - 1, pageSize }),
            nextPageLink = linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber + 1, pageSize }),
            totalCount,
            pageSize,
            currentPage = pageNumber,
            totalPages = Math.Ceiling(1f * totalCount / pageSize),
        };
        Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationInfo));
        var entities = userRepository.GetPage(pageNumber, pageSize);
        return Ok(mapper.Map<IEnumerable<UserDto>>(entities));
    }

    [HttpOptions]
    public IActionResult Options()
    {
        var allowedMethods = new[] { HttpMethods.Get, HttpMethods.Options, HttpMethods.Post };
        Response.Headers.Add("Allow", string.Join(", ", allowedMethods));
        return Ok();
    }
}