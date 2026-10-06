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
    private readonly IMapper mapper;

    private readonly IUserRepository userRepository;
    private readonly LinkGenerator linkGenerator;

    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
        this.linkGenerator = linkGenerator;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public IActionResult GetUserById([FromRoute] Guid userId)
    {
        if (!TryFindUser(userId, out var user)) return NotFound();
        return Ok(mapper.Map<UserDto>(user));
    }

    [HttpHead("{userId}")]
    [Produces("application/json")]
    public IActionResult Head([FromRoute] Guid userId)
    {
        if (!TryFindUser(userId, out _)) return NotFound();
        Response.ContentType = "application/json; charset=utf-8";
        return Ok();
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] PostCreateUserDto? user)
    {
        if (user is null)
        {
            return BadRequest();
        }

        if (!ValidateData(user))
        {
            return UnprocessableEntity(ModelState);
        }

        var createdUserEntity = userRepository.Insert(mapper.Map<UserEntity>(user));
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpsertUser([FromRoute] Guid userId, [FromBody] PutUpsertUserDto? user)
    {
        if (!ValidateData(userId) || user == null)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var userEntity = mapper.Map(user, new UserEntity(userId));
        userRepository.UpdateOrInsert(userEntity, out var isInserted);
        return isInserted
            ? CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id)
            : NoContent();
    }

    [HttpPatch("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<PutUpsertUserDto>? patchDoc)
    {
        if (patchDoc is null)
        {
            return BadRequest();
        }

        if (!ValidateData(userId) || !TryFindUser(userId, out var user))
        {
            return NotFound();
        }

        var initialDto = mapper.Map<PutUpsertUserDto>(user);
        patchDoc.ApplyTo(initialDto, ModelState);
        if (!ValidateData(initialDto))
        {
            return UnprocessableEntity(ModelState);
        }

        userRepository.Insert(mapper.Map<UserEntity>(initialDto));
        return NoContent();
    }

    [HttpDelete("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (!TryFindUser(userId, out var user)) return NotFound();
        userRepository.Delete(userId);
        return NoContent();
    }

    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 20);
        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);
        Response.Headers.Append("X-Pagination", GeneratePageLink(pageList, pageNumber, pageSize));
        return Ok(users);
    }

    [HttpOptions]
    [Produces("application/json", "application/xml")]
    public IActionResult Options()
    {
        Response.Headers.Append("Allow", "POST, GET, OPTIONS");
        return Ok();
    }

    private string GeneratePageLink(PageList<UserEntity> pageList, int pageNumber, int pageSize)
    {
        var totalCount = pageList.TotalCount;
        string? previousLink = null;
        if (pageNumber > 1)
        {
            previousLink = linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new
            {
                pageNumber = pageNumber - 1,
                pageSize = pageSize
            });
        }

        var nextLink = linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new
        {
            pageNumber = pageNumber + 1,
            pageSize = pageSize
        });
        var paginationHeader = new
        {
            previousPageLink = previousLink,
            nextPageLink = nextLink,
            pageSize = pageSize,
            currentPage = pageNumber,
            totalCount = totalCount,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
        return JsonConvert.SerializeObject(paginationHeader);
    }

    private bool TryFindUser(Guid userId, out UserEntity? user)
    {
        user = userRepository.FindById(userId);
        return user != null;
    }

    private bool ValidateData(Guid userId)
    {
        return userId != Guid.Empty;
    }

    private bool ValidateData(PutUpsertUserDto putUpsertDto)
    {
        if (string.IsNullOrEmpty(putUpsertDto.FirstName))
        {
            ModelState.AddModelError("firstName", "Invalid First Name");
            return false;
        }

        if (string.IsNullOrEmpty(putUpsertDto.LastName))
        {
            ModelState.AddModelError("lastName", "Invalid Last Name");
            return false;
        }

        if (!TryValidateModel(putUpsertDto) || !ModelState.IsValid)
        {
            return false;
        }

        return true;
    }

    private bool ValidateData(PostCreateUserDto? user)
    {
        if (!string.IsNullOrEmpty(user.Login) && user.Login.All(char.IsLetterOrDigit)) return true;
        ModelState.AddModelError("login", "Invalid login");
        return false;
    }
}