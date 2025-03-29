using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private const int MaxPageSize = 20;
    private const int DefaultPageSize = 10;

    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;
    private readonly LinkGenerator linkGenerator;

    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
        this.linkGenerator = linkGenerator;
    }

    [HttpHead("{userId}", Name = nameof(GetUserById))]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
        {
            return NotFound();
        }

        if (HttpContext.Request.Method == HttpMethod.Head.ToString())
        {
            Response.ContentType = "application/json; charset=utf-8";
            return Ok();
        }

        var result = mapper.Map<UserDto>(user);
        return Ok(result);
    }

    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto[]> GetUsers([FromQuery] int pageNumber, [FromQuery] int pageSize = DefaultPageSize)
    {
        pageNumber = pageNumber > 0 ? pageNumber : 1;
        pageSize = Math.Max(Math.Min(MaxPageSize, pageSize), 1);
        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);

        var paginationHeader = new
        {
            previousPageLink = pageList.HasPrevious
                ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber - 1, pageSize })
                : null,
            nextPageLink = pageList.HasNext
                ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber + 1, pageSize })
                : null,
            totalCount = pageList.TotalCount,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages
        };

        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        return Ok(users);
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] CreateUserRequest? createUserRequest)
    {
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        if (createUserRequest == null)
            return BadRequest();

        if (!createUserRequest.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError(nameof(createUserRequest.Login), "Invalid login");
            return UnprocessableEntity(ModelState);
        }

        var userEntity = mapper.Map<UserEntity>(createUserRequest);
        var user = userRepository.Insert(userEntity);

        return CreatedAtRoute(nameof(GetUserById), new { userId = user.Id }, user.Id);
    }

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult Update([FromBody] UpdateUserRequest? updateUserRequest, [FromRoute] Guid userId)
    {
        if (updateUserRequest == null || userId == Guid.Empty)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = mapper.Map(updateUserRequest, new UserEntity(userId));

        userRepository.UpdateOrInsert(userEntity, out var isInserted);

        if (isInserted)
            return CreatedAtRoute(nameof(GetUserById), new { userId = userEntity.Id }, userEntity.Id);
        return NoContent();
    }

    [HttpPatch("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser([FromBody] JsonPatchDocument<UpdateUserRequest>? patchDocument, [FromRoute] Guid userId)
    {
        if (patchDocument == null)
            return BadRequest();

        if (userId == Guid.Empty)
            return NotFound();

        var existingUser = userRepository.FindById(userId);
        if (existingUser == null)
            return NotFound();

        var updatedUser = mapper.Map<UpdateUserRequest>(existingUser);
        patchDocument.ApplyTo(updatedUser, ModelState);

        if (!TryValidateModel(updatedUser))
            return UnprocessableEntity(ModelState);

        var userEntity = mapper.Map(updatedUser, new UserEntity(userId));
        userRepository.Update(userEntity);
        return NoContent();
    }

    [HttpDelete("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (userId == Guid.Empty)
            return NotFound();

        if (userRepository.FindById(userId) == null)
            return NotFound();

        userRepository.Delete(userId);

        return NoContent();
    }

    [HttpOptions]
    [Produces("application/json", "application/xml")]
    public IActionResult GetOptions()
    {
        var allowedMethods = new[] { HttpMethod.Get, HttpMethod.Post, HttpMethod.Options };
        var allowHeaderValue = string.Join(",", allowedMethods.Select(m => m.Method));
        Response.Headers.Add("Allow", allowHeaderValue);
        return Ok();
    }
}