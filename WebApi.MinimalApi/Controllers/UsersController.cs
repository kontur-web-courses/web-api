using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
using AutoMapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.JsonPatch;

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
        this.mapper = mapper;
        this.linkGenerator = linkGenerator;
    }

    [Produces("application/json", "application/xml")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [HttpHead("{userId}")]
    public IActionResult GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
            return NotFound();
        var response = mapper.Map<UserDto>(user);
        if (HttpMethods.IsHead(Request.Method))
        {
            var contentType = Request.Headers.Accept.ToString().Contains("application/xml")
                ? "application/xml; charset=utf-8"
                : "application/json; charset=utf-8";

            return new ContentResult { ContentType = contentType, Content = string.Empty };
        }

        return Ok(response);
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] CreateUserDto user)
    {
        if (user is null)
            return BadRequest();
        var userEntity = mapper.Map<UserEntity>(user);

        if (string.IsNullOrEmpty(userEntity.Login))
            ModelState.AddModelError("login", "Login cannot be empty");
        else if (!userEntity.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError("login", "Unacceptable chars in login");


        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        userEntity = userRepository.Insert(userEntity);

        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = userEntity.Id },
            mapper.Map<UserDto>(userEntity));
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (userRepository.FindById(userId) is null)
        {
            return NotFound();
        }

        userRepository.Delete(userId);
        return NoContent();
    }

    [Produces("application/json", "application/xml")]
    [HttpGet(Name = nameof(GetUsers))]
    public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 20);

        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);

        string? previousPageLink = null;
        string? nextPageLink = null;

        if (pageList.HasPrevious)
        {
            previousPageLink = linkGenerator.GetUriByRouteValues(
                HttpContext,
                nameof(GetUsers),
                new { pageNumber = pageList.CurrentPage - 1, pageSize = pageList.PageSize });
        }

        if (pageList.HasNext)
        {
            nextPageLink = linkGenerator.GetUriByRouteValues(
                HttpContext,
                nameof(GetUsers),
                new { pageNumber = pageList.CurrentPage + 1, pageSize = pageList.PageSize });
        }

        var paginationHeader = new
        {
            previousPageLink,
            nextPageLink,
            totalCount = pageList.TotalCount,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages
        };

        Response.Headers["X-Pagination"] = JsonConvert.SerializeObject(paginationHeader);

        return Ok(users);
    }

    [HttpPut("{userId}")]
    public IActionResult UpdateUser(
        [FromRoute] string userId,
        [FromBody] UpdateUserDto user)
    {
        if (!Guid.TryParse(userId, out var id))
            return BadRequest();
        if (user is null)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var userEntity = new UserEntity(id);
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


    [HttpPatch("{userId}")]
       [Consumes("application/json-patch+json")]
    public IActionResult PartiallyUpdateUser(
        [FromRoute] Guid userId,
        [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc)
    {
        if (patchDoc is null)
            return BadRequest();

        var existingUser = userRepository.FindById(userId);
        if (existingUser is null)
            return NotFound();

        var update = mapper.Map<UpdateUserDto>(existingUser);
        patchDoc.ApplyTo(update, ModelState);
        TryValidateModel(update);

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        mapper.Map(update, existingUser);
        userRepository.Update(existingUser);
        return NoContent();
    }

    [HttpOptions]
    public IActionResult GetOptions()
    {
        Response.Headers.Add("Allow", "GET, POST, OPTIONS");
        return Ok();
    }
}
