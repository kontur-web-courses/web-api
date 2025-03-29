using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.Swagger.Annotations;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

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
        this.mapper = mapper;
        this.linkGenerator = linkGenerator;
    }

    [HttpGet("{userId:guid}", Name = nameof(GetUserById))]
    [HttpHead("{userId:guid}")]
    [SwaggerResponse(200, "OK", typeof(UserDto))]
    [SwaggerResponse(404, "User not found")]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);

        if (user == null)
            return NotFound();

        return Ok(mapper.Map<UserDto>(user));
    }

    [HttpPost]
    [SwaggerResponse(201, "User created")]
    [SwaggerResponse(400, "Incorrect input data")]
    [SwaggerResponse(422, "Validation error")]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] UserCreationDto? userDto)
    {
        if (userDto == null)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        if (!userDto.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("Login", "Login should contain only letters or digits");
            return UnprocessableEntity(ModelState);
        }

        var user = mapper.Map<UserEntity>(userDto);
        user = userRepository.Insert(user);

        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = user.Id },
            user.Id);
    }

    [HttpPut("{userId}")]
    [SwaggerResponse(201, "User created")]
    [SwaggerResponse(204, "User updated")]
    [SwaggerResponse(400, "Incorrect input data")]
    [SwaggerResponse(422, "Validation error")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromBody] UserUpdateDto? userDto, [FromRoute] Guid userId)
    {
        if (userDto == null || userId == Guid.Empty)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var user = mapper.Map(userDto, new UserEntity(userId));
        userRepository.UpdateOrInsert(user, out var isInserted);

        return isInserted
            ? CreatedAtRoute(
                nameof(GetUserById),
                new { userId = user.Id },
                user.Id)
            : NoContent();
    }

    [HttpPatch("{userId:guid}")]
    [SwaggerResponse(204, "User updated")]
    [SwaggerResponse(400, "Incorrect input data")]
    [SwaggerResponse(404, "User not found")]
    [SwaggerResponse(422, "Validation error")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser([FromBody] JsonPatchDocument<UserUpdateDto>? patchDoc, [FromRoute] Guid userId)
    {
        if (patchDoc == null)
            return BadRequest();

        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();

        var userDto = mapper.Map<UserUpdateDto>(user);
        patchDoc.ApplyTo(userDto, ModelState);

        if (!TryValidateModel(userDto))
            return UnprocessableEntity(ModelState);

        user = mapper.Map<UserEntity>(userDto);
        userRepository.Update(user);
        return NoContent();
    }

    [HttpDelete("{userId:guid}")]
    [SwaggerResponse(204, "User deleted")]
    [SwaggerResponse(404, "User not found")]
    [Produces("application/json", "application/xml")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();

        userRepository.Delete(userId);
        return NoContent();
    }

    [HttpGet(Name = nameof(GetUsers))]
    [ProducesResponseType<IEnumerable<UserDto>>(200)]
    [Produces("application/json", "application/xml")]
    public ActionResult<IEnumerable<UserDto>> GetUsers([FromQuery] int? pageNumber, [FromQuery] int? pageSize)
    {
        var intPageNumber = pageNumber != null
            ? Math.Max(1, pageNumber.Value)
            : 1;

        var intPageSize = pageSize != null
            ? Math.Min(20, Math.Max(1, pageSize!.Value))
            : 10;

        var pageList = userRepository.GetPage(intPageNumber, intPageSize);

        var paginationHeader = new
        {
            previousPageLink = pageList.HasPrevious
                ? GetUriUsers(intPageNumber - 1, intPageSize)
                : null,
            nextPageLink = pageList.HasNext
                ? GetUriUsers(intPageNumber + 1, intPageSize)
                : null,
            totalCount = pageList.TotalCount,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages,
        };

        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

        return Ok(paginationHeader);
    }

    [HttpOptions]
    [SwaggerResponse(200, "OK")]
    [Produces("application/json", "application/xml")]
    public IActionResult Options()
    {
        Response.Headers.Add("Allow", "POST, GET, OPTIONS");
        return Ok();
    }

    private string? GetUriUsers(int pageNumber, int pageSize)
        => linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageSize, pageNumber });
}