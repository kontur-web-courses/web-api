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

    [HttpGet(Name = nameof(GetUsers))]
    public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 20);

        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var paginationHeader = new
        {
            previousPageLink = pageList.HasPrevious
                ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new
                {
                    pageNumber = pageNumber - 1,
                    pageSize
                })
                : null,
            nextPageLink = pageList.HasNext
                ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new
                {
                    pageNumber = pageNumber + 1,
                    pageSize
                })
                : null,
            totalCount = pageList.TotalCount,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages
        };
        Response.Headers["X-Pagination"] = JsonConvert.SerializeObject(paginationHeader);

        return Ok(mapper.Map<IEnumerable<UserDto>>(pageList));
    }

    [HttpOptions]
    public IActionResult GetUsersOptions()
    {
        Response.Headers["Allow"] = "POST, GET, OPTIONS";
        return Ok();
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();

        return Ok(mapper.Map<UserDto>(user));
    }

    [HttpHead("{userId}")]
    public IActionResult HeadUserById([FromRoute] Guid userId)
    {
        if (userRepository.FindById(userId) == null)
            return NotFound();

        Response.ContentType = "application/json; charset=utf-8";
        return StatusCode(StatusCodes.Status200OK);
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] CreateUserDto? user)
    {
        if (user == null)
            return BadRequest();

        if (!string.IsNullOrEmpty(user.Login) && !user.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError(nameof(CreateUserDto.Login), "Login should contain only letters or digits");

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var createdUserEntity = userRepository.Insert(mapper.Map<UserEntity>(user));
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            mapper.Map<UserDto>(createdUserEntity));
    }

    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdateUserDto? user)
    {
        if (user == null || userId == Guid.Empty)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = mapper.Map(user, new UserEntity(userId));
        userRepository.UpdateOrInsert(userEntity, out var isInserted);

        if (!isInserted)
            return NoContent();

        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = userEntity.Id },
            userEntity.Id);
    }

    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserDto>? patchDoc)
    {
        if (patchDoc == null)
            return BadRequest();

        var userEntity = userRepository.FindById(userId);
        if (userEntity == null)
            return NotFound();

        var updateDto = mapper.Map<UpdateUserDto>(userEntity);
        patchDoc.ApplyTo(updateDto, ModelState);
        TryValidateModel(updateDto);

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        userRepository.Update(mapper.Map(updateDto, userEntity));
        return NoContent();
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (userRepository.FindById(userId) == null)
            return NotFound();

        userRepository.Delete(userId);
        return NoContent();
    }
}
