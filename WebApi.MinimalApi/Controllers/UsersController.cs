using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json", "application/xml")]
public class UsersController : Controller
{
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;
    private readonly LinkGenerator linkGenerator;
    private static readonly string[] UsersAllowedMethods = { "POST", "GET", "OPTIONS" };

    public UsersController(IUserRepository userRepository, IMapper mapper,
        LinkGenerator linkGenerator)
    {
        this.userRepository = userRepository ??
                              throw new ArgumentException("Null reference", nameof(userRepository));
        this.mapper = mapper;
        this.linkGenerator = linkGenerator;
    }

    [HttpHead("{userId}")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);

        if (user is null)
        {
            return NotFound();
        }

        if (!HttpContext.Request.Method.Equals("head", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(mapper.Map<UserDto>(user));
        }

        Response.ContentType = HttpConstants.ContentTypeJsonHeader;
        return Ok();
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] CreateUserRequest? user)
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
            ModelState.AddModelError(nameof(user.Login), "Login has invalid chars");
            return UnprocessableEntity(ModelState);
        }

        var entity = userRepository.Insert(mapper.Map<UserEntity>(user));


        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = entity.Id },
            entity.Id);
    }

    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromBody] UpdateUserRequest? user, [FromRoute] Guid userId)
    {
        if (user is null || userId == Guid.Empty)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var entity = mapper.Map(new UserEntity(userId), mapper.Map<UserEntity>(user));
        userRepository.UpdateOrInsert(entity, out var isInserted);
        if (isInserted)
        {
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = entity.Id },
                entity.Id);
        }

        return NoContent();
    }

    [HttpPatch("{userId}")]
    public IActionResult PatchUser([FromBody] JsonPatchDocument<PatchUserRequest>? patchDoc, [FromRoute] Guid userId)
    {
        if (patchDoc is null)
        {
            return BadRequest();
        }

        if (userId == Guid.Empty)
        {
            return NotFound();
        }

        var patch = new PatchUserRequest();
        patchDoc.ApplyTo(patch);

        if (!TryValidateModel(patch) || !ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var userEntity = mapper.Map(new UserEntity(userId), mapper.Map<UserEntity>(patch));
        userRepository.Update(userEntity);
        if (userRepository.FindById(userId) is null)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return NotFound();
        }

        if (userRepository.FindById(userId) == null)
        {
            return NotFound();
        }

        userRepository.Delete(userId);
        return NoContent();
    }

    [HttpGet]
    public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = pageSize < 1 ? 1 : pageSize > 20 ? 20 : pageSize;

        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var users = mapper.Map<IList<UserDto>>(pageList);
        var paginationHeader = new
        {
            previousPageLink = pageList.HasPrevious
                ? linkGenerator.GetUriByRouteValues(HttpContext, "", new { pageNumber = pageNumber - 1, pageSize })
                : null,
            nextPageLink = pageList.HasNext
                ? linkGenerator.GetUriByRouteValues(HttpContext, "", new { pageNumber = pageNumber + 1, pageSize })
                : null,
            totalCount = pageList.TotalCount,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages,
        };
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        return Ok(users);
    }

    [HttpOptions]
    public IActionResult GetUsersOptions()
    {
        Response.Headers.Append(HttpConstants.AllowHeader, UsersAllowedMethods);
        return Ok();
    }
}