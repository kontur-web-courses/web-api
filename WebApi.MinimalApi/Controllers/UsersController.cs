using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация

    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly LinkGenerator _linkGenerator;

    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _linkGenerator = linkGenerator;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user is null)
            return NotFound();
        var dto = _mapper.Map<UserDto>(user);
        return Ok(dto);
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] CreateUserDto user)
    {
        if (user is null)
            return BadRequest();

        if (user.Login != null && user.Login.Any(c => !char.IsLetterOrDigit(c)))
            ModelState.AddModelError("Login", "Login should contain only letters or digits");

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var entity = _mapper.Map<UserEntity>(user);
        var createdUser = _userRepository.Insert(entity);
        var createdUserDto = _mapper.Map<UserDto>(createdUser);

        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUser.Id },
            createdUserDto
        );
    }

    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromRoute] string userId, [FromBody] UpdateUserDto user)
    {
        if (!Guid.TryParse(userId, out var id))
            return BadRequest();

        if (user is null)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var entity = _userRepository.FindById(id) ?? new UserEntity(id);
        _mapper.Map(user, entity);
        _userRepository.UpdateOrInsert(entity, out var isInserted);

        if (isInserted)
        {
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = id },
                id
            );
        }

        return NoContent();
    }

    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser([FromRoute] string userId, [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc)
    {
        if (!Guid.TryParse(userId, out var id))
            return NotFound();

        if (patchDoc is null)
            return BadRequest();

        var entity = _userRepository.FindById(id);

        if (entity is null)
            return NotFound();

        var updateDto = _mapper.Map<UpdateUserDto>(entity);
        patchDoc.ApplyTo(updateDto, ModelState);
        TryValidateModel(updateDto);

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        _mapper.Map(updateDto, entity);
        _userRepository.Update(entity);

        return NoContent();
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] string userId)
    {
        if (!Guid.TryParse(userId, out var id))
            return NotFound();

        var user = _userRepository.FindById(id);
        if (user is null)
            return NotFound();

        _userRepository.Delete(id);
        return NoContent();
    }

    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult HeadUserById([FromRoute] string userId)
    {
        if (!Guid.TryParse(userId, out var id))
            return NotFound();

        var user = _userRepository.FindById(id);
        if (user is null)
            return NotFound();

        Response.ContentType = "application/json; charset=utf-8";
        return Ok();
    }


    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 20);
        var pageList = _userRepository.GetPage(pageNumber, pageSize);
        var users = _mapper.Map<IEnumerable<UserDto>>(pageList);
        string? previousPageLink = null;
        string? nextPageLink = null;

        if (pageList.HasPrevious)
        {
            previousPageLink =
                _linkGenerator.GetUriByRouteValues(HttpContext,
                    nameof(GetUsers),
                    new
                    {
                        pageNumber = pageList.CurrentPage - 1,
                        pageSize = pageList.PageSize
                    });
        }

        if (pageList.HasNext)
        {
            nextPageLink =
                _linkGenerator.GetUriByRouteValues(HttpContext,
                    nameof(GetUsers),
                    new
                    {
                        pageNumber = pageList.CurrentPage + 1,
                        pageSize = pageList.PageSize
                    });
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

    [HttpOptions]
    public IActionResult GetUsersOptions()
    {
        Response.Headers.Append("Allow", "GET, POST, OPTIONS");
        return Ok();
    }
}