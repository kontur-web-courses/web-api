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
    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
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
    [Produces("application/json", "application/xml")]
    public ActionResult<IEnumerable<UserDto>> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 20);

        var pageList = userRepository.GetPage(pageNumber, pageSize);
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
        Response.Headers["X-Pagination"] = JsonConvert.SerializeObject(paginationHeader);

        return Ok(mapper.Map<IEnumerable<UserDto>>(pageList));
    }

    [HttpOptions]
    public IActionResult GetUsersOptions()
    {
        Response.Headers.Append("Allow", "POST, GET, OPTIONS");
        return Ok();
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);

        if (user is null)
            return NotFound();

        return Ok(mapper.Map<UserDto>(user));
    }

    [HttpHead("{userId}", Name = nameof(HeadUserById))]
    public IActionResult HeadUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);

        if (user is null)
            return NotFound();

        Response.ContentType = "application/json; charset=utf-8";
        return Ok();
    }


    [HttpPost]
    public IActionResult CreateUser([FromBody] UserCreateDto? userCreate)
    {
        if (userCreate is null)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var createdUserEntity = userRepository.Insert(mapper.Map<UserEntity>(userCreate));
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }

    [HttpPut("{userId}")]
    public IActionResult UpdateUser(Guid userId, [FromBody] UserUpdateDto? userUpdate)
    {
        if (userUpdate is null | userId.Equals(Guid.Empty))
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = mapper.Map(
            userUpdate,
            new UserEntity(userId));

        userRepository.UpdateOrInsert(userEntity, out var isInserted);

        if (isInserted)
        {
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId },
                userId);
        }

        return NoContent();
    }

    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser(Guid userId, [FromBody] JsonPatchDocument<UserUpdateDto>? patchDoc)
    {
        if (userId.Equals(Guid.Empty))
            return NotFound();

        if (patchDoc is null)
            return BadRequest();

        var userEntity = userRepository.FindById(userId);

        if (userEntity is null)
            return NotFound();

        var updateDto = mapper.Map<UserUpdateDto>(userEntity);

        patchDoc.ApplyTo(updateDto, ModelState);

        if (!TryValidateModel(updateDto))
            return UnprocessableEntity(ModelState);

        mapper.Map(updateDto, userEntity);

        userRepository.Update(userEntity);

        return NoContent();
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser(Guid userId)
    {
        var userEntity = userRepository.FindById(userId);

        if (userEntity is null)
            return NotFound();

        userRepository.Delete(userId);

        return NoContent();
    }
}
