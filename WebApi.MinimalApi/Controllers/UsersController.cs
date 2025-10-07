using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json", "application/xml")]
[Consumes("application/json", "application/xml")]
public class UsersController : Controller
{
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;
    private readonly LinkGenerator linkGenerator;
        
    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
        this.linkGenerator = linkGenerator;
    }

    [HttpGet("{userId:guid}", Name = nameof(GetUserById))]
    [HttpHead("{userId:guid}")]
    [SwaggerResponse(StatusCodes.Status200OK, "OK", typeof(UserDto))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "User not found")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
            return NotFound();
        if (Request.Method == HttpMethods.Head)
        {
            Response.ContentType = "application/json; charset=utf-8";
            return Ok();
        }
        var userDto = mapper.Map<UserDto>(user);
        return Ok(userDto);
    }

    [HttpPost]
    [SwaggerResponse(StatusCodes.Status201Created, "User successfully created; returns its ID", typeof(Guid))]
    [SwaggerResponse(StatusCodes.Status422UnprocessableEntity, "Model validation failed")]
    public IActionResult CreateUser([FromBody] CreateUserRequest? userRequest)
    {
        if (userRequest is null)
            return BadRequest();
        if (string.IsNullOrEmpty(userRequest.Login) || userRequest.Login.Any(c => !char.IsLetterOrDigit(c)))
            ModelState.AddModelError(nameof(CreateUserRequest.Login), 
                "Login must consist only of letters and digits and should not be empty");
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var user = mapper.Map<UserEntity>(userRequest);
        var insertedUser = userRepository.Insert(user);
        return CreatedAtRoute(nameof(GetUserById), new { userId = insertedUser.Id }, insertedUser.Id);
    }
    
    [HttpPut("{userId}")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, $"Invalid {nameof(userId)} or request body")]
    [SwaggerResponse(StatusCodes.Status422UnprocessableEntity, "Model validation failed")]
    [SwaggerResponse(StatusCodes.Status201Created, 
        "User not found; a new user was created with request data; returns its ID", typeof(Guid))]
    [SwaggerResponse(StatusCodes.Status204NoContent, "User successfully updated")]
    public IActionResult UpsertUser([FromRoute] string userId, [FromBody] UpsertUserRequest? userRequest)
    {
        if (userRequest is null || !Guid.TryParse(userId, out var guidUserId))
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var user = mapper.Map(userRequest, new UserEntity(guidUserId));
        userRepository.UpdateOrInsert(user, out var isInserted);
        return isInserted 
            ? CreatedAtRoute(nameof(GetUserById), new {userId = user.Id}, user.Id) 
            : NoContent();
    }

    [HttpPatch("{userId:guid}")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "User not found")]
    [SwaggerResponse(StatusCodes.Status422UnprocessableEntity, "Model validation failed")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "User successfully updated")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<PartiallyUpdateUserRequest>? request)
    {
        if (request is null)
            return BadRequest();
        var user = userRepository.FindById(userId);
        if (user is null)
            return NotFound();
        var partiallyUpdateRequest = mapper.Map<PartiallyUpdateUserRequest>(user);
        request.ApplyTo(partiallyUpdateRequest);
        TryValidateModel(partiallyUpdateRequest);
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var updatedUser = mapper.Map(partiallyUpdateRequest, user);
        userRepository.Update(updatedUser);
        return NoContent();
    }

    [HttpDelete("{userId:guid}")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "User not found")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "User successfully deleted")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
            return NotFound();
        userRepository.Delete(userId);
        return NoContent();
    }

    [HttpGet(Name = nameof(GetAllUsers))]
    [SwaggerResponse(StatusCodes.Status200OK, 
        "Returns a paginated list of users. Pagination metadata is included in the X-Pagination header")]
    public IActionResult GetAllUsers([FromQuery] GetUsersRequest request)
    {
        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 20);
        var usersPage = userRepository.GetPage(pageNumber, pageSize);
        var nextPageLink = usersPage.HasNext
            ? linkGenerator.GetUriByRouteValues(
                HttpContext, nameof(GetAllUsers), new GetUsersRequest { PageNumber = pageNumber + 1, PageSize = pageSize })
            : null;
        var previousPageLink = usersPage.HasPrevious
            ? linkGenerator.GetUriByRouteValues(
                HttpContext, nameof(GetAllUsers), new GetUsersRequest { PageNumber = pageNumber - 1, PageSize = pageSize })
            : null;
        var paginationHeader = new
        {
            previousPageLink = previousPageLink,
            nextPageLink = nextPageLink,
            totalCount = usersPage.TotalCount,
            pageSize = usersPage.PageSize,
            currentPage = usersPage.CurrentPage,
            totalPages = usersPage.TotalPages,
        };
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        return Ok(mapper.Map<IEnumerable<UserDto>>(usersPage));
    }

    [HttpOptions]
    [SwaggerResponse(StatusCodes.Status200OK, "Returns allowed HTTP methods in the Allow header")]
    public IActionResult GetOptions()
    {
        Response.Headers.Append("Allow", "GET, POST, OPTIONS");
        return Ok();
    }
}