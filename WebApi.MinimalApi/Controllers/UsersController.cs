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

    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
        this.linkGenerator = linkGenerator;
    }

    [HttpGet("{userId:guid}", Name = nameof(GetUserById))]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);

        if (user == null)
        {
            return NotFound();
        }

        var result = mapper.Map<UserDto>(user);

        return Ok(result);
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] CreateUserDto? user)
    {
        if (user is null)
            return BadRequest();

        if (string.IsNullOrWhiteSpace(user.Login))
            ModelState.AddModelError("Login", "Login is required.");
        if (!string.IsNullOrEmpty(user.Login) && !user.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError("Login", "Login must contain only letters and digits.");

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var entity = mapper.Map<UserEntity>(user);
        var created = userRepository.Insert(entity);

        return CreatedAtRoute(
            routeName: nameof(GetUserById),
            routeValues: new { userId = created.Id },
            value: created.Id
        );
    }

    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromRoute] string userId, [FromBody] UpdateUserDto? user)
    {
        if (!Guid.TryParse(userId, out var id) || user is null)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var entity = userRepository.FindById(id);
        if (entity is null)
            return CreateUser(mapper.Map<CreateUserDto>(user));

        mapper.Map(user, entity);
        userRepository.Update(entity);

        return NoContent();
    }

    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser([FromBody] JsonPatchDocument<UpdateUserDto>? patchDocument, [FromRoute] string userId)
    {
        if (patchDocument is null)
            return BadRequest();

        if (!Guid.TryParse(userId, out var id))
            return NotFound();

        var entity = userRepository.FindById(id);
        if (entity is null)
            return NotFound();

        var modelToPatch = mapper.Map<UpdateUserDto>(entity);

        patchDocument.ApplyTo(modelToPatch, ModelState);
        TryValidateModel(modelToPatch);

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        mapper.Map(modelToPatch, entity);
        userRepository.Update(entity);

        return NoContent();
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] string userId)
    {
        if (!Guid.TryParse(userId, out var id) || userRepository.FindById(id) is null)
            return NotFound();

        userRepository.Delete(id);
        return NoContent();
    }

    [HttpHead("{userId}")]
    public IActionResult HeadUser([FromRoute] Guid userId)
    {
        var isExists = userRepository.FindById(userId) is not null;
        Response.Body = Stream.Null;
        return isExists
            ? Ok(isExists)
            : NotFound();
    }

    [HttpGet]
    public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 20);

        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);

        var previousPageLink = pageList.HasPrevious
            ? linkGenerator.GetUriByAction(HttpContext, nameof(GetUsers), values: new { pageNumber = pageNumber - 1, pageSize })
            : null;
        var nextPageLink = pageList.HasNext
            ? linkGenerator.GetUriByAction(HttpContext, nameof(GetUsers), values: new { pageNumber = pageNumber + 1, pageSize })
            : null;

        var totalCount = pageList.TotalCount;
        var currentPage = pageNumber;
        var totalPages = (int)Math.Ceiling((double)pageList.TotalCount / pageSize);

        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(new
        {
            previousPageLink,
            nextPageLink,
            totalCount,
            pageSize,
            currentPage,
            totalPages
        }));

        return Ok(users);
    }

    [HttpOptions]
    public IActionResult Options()
    {
        Response.Headers.Append("Allow", "GET, POST, OPTIONS");

        return Ok();
    }
}