using System.Diagnostics;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json", "application/xml")]
public class UsersController : Controller
{
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;
    private readonly LinkGenerator linkGenerator;
    private readonly EndpointDataSource dataSource;

    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator, EndpointDataSource dataSource)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
        this.linkGenerator = linkGenerator;
        this.dataSource = dataSource;
    }

    [HttpHead("{userId}")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    public ActionResult<UserResponse> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
        {
            return NotFound();
        }

        var userDto = mapper.Map<UserResponse>(user);

        if (HttpMethods.IsHead(Request.Method))
        {
            Response.Headers.Append("Content-Type", "application/json; charset=utf-8");
            return Ok();
        }

        return userDto;
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] UserCreationRequest? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var user = mapper.Map<UserEntity>(request);

        var createdUser = userRepository.Insert(user);

        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUser.Id },
            createdUser.Id);
    }

    [HttpPut("{userId}")]
    public IActionResult UpsertUser([FromRoute] Guid userId, [FromBody] UserUpdateRequest? request)
    {
        if (request is null || userId == Guid.Empty)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var user = mapper.Map(request, new UserEntity(userId));
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

    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserUpdateRequest>? patchDoc)
    {
        if (patchDoc is null)
        {
            return BadRequest();
        }

        var user = userRepository.FindById(userId);
        if (user is null)
        {
            return NotFound();
        }

        var userUpdate = mapper.Map<UserUpdateRequest>(user);
        patchDoc.ApplyTo(userUpdate, ModelState);

        if (!TryValidateModel(userUpdate))
        {
            return UnprocessableEntity(ModelState);
        }

        var updatedUser = mapper.Map<UserEntity>(userUpdate);
        userRepository.Update(updatedUser);

        return NoContent();
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
        {
            return NotFound();
        }

        userRepository.Delete(userId);

        return NoContent();
    }

    [HttpGet(Name = nameof(GetUsers))]
    public IActionResult GetUsers([FromQuery] UsersGetRequest request)
    {
        var pageSize = request.PageSize switch
        {
            < 1 => 1,
            > 20 => 20,
            _ => request.PageSize
        };

        var pageNumber = request.PageNumber switch
        {
            < 1 => 1,
            _ => request.PageNumber
        };

        var pageOfUsers = userRepository.GetPage(pageNumber, pageSize);
        var users = mapper.Map<IEnumerable<UserResponse>>(pageOfUsers);

        var paginationHeader = new
        {
            previousPageLink = GetPageLink(pageOfUsers.PageSize, pageOfUsers.CurrentPage - 1),
            nextPageLink = GetPageLink(pageOfUsers.PageSize, pageOfUsers.CurrentPage + 1),
            totalCount = pageOfUsers.TotalCount,
            pageSize = pageOfUsers.PageSize,
            currentPage = pageOfUsers.CurrentPage,
            totalPages = pageOfUsers.TotalPages,
        };

        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

        return Ok(users);
    }

    private string? GetPageLink(int pageSize, int pageNumber)
    {
        if (pageNumber is < 1 or > 20)
        {
            return null;
        }

        return linkGenerator.GetUriByRouteValues(
            HttpContext,
            nameof(GetUsers),
            new UsersGetRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            });
    }

    [HttpOptions]
    public IActionResult GetOptions()
    {
        var methods = dataSource.Endpoints
            .SelectMany(e => e.Metadata.OfType<IHttpMethodMetadata>())
            .SelectMany(m => m.HttpMethods)
            .Distinct()
            .ToArray();

        Response.Headers.Append("Allow", string.Join(", ", methods));

        return Ok();
    }
}
