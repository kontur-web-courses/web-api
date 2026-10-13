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
    private const int MinPageNumber = 1;
    private const int MinPageSize = 1;
    private const int MaxPageSize = 20;

    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;

    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] CreateUserRequest user)
    {
        if (user is null)
            return BadRequest();

        if (!string.IsNullOrEmpty(user.Login) && !user.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError(nameof(user.Login), "Login should contain only letters or digits");

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = mapper.Map<UserEntity>(user);
        var createdUserEntity = userRepository.Insert(userEntity);

        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }

    [HttpDelete("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
            return NotFound();

        userRepository.Delete(userId);
        return NoContent();
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var entity = userRepository.FindById(userId);
        if (entity == null) return NotFound();

        var dto = mapper.Map<UserDto>(entity);

        if (HttpMethods.IsHead(Request.Method))
        {
            Response.ContentType = "application/json; charset=utf-8";
            return Ok();
        }

        return Ok(dto);         
    }


    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdateUserDto? user)
    {
        if (user == null) return BadRequest();
        if (!ModelState.IsValid)
        {
            if (ModelState.TryGetValue("userId", out var entry) && entry.Errors.Count > 0)
                return BadRequest();
            return UnprocessableEntity(ModelState);
        }

        var currentUser = userRepository.FindById(userId);

        if (currentUser == null)
        {
            var newUser = new UserEntity(userId);
            mapper.Map(user, newUser);

            userRepository.UpdateOrInsert(newUser, out var isInserted);
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = newUser.Id },
                new UserIdDto { Id = newUser.Id });
        }
        mapper.Map(user, currentUser);
        userRepository.UpdateOrInsert(currentUser, out var inserted);

        return NoContent();
    }
    
    [HttpPatch("{userId}")]
    [Produces("application/json")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc)
    {
        if (patchDoc is null)
            return BadRequest();
        if (ModelState.TryGetValue("userId", out var entry) && entry.Errors.Count > 0)
            return NotFound();
        
        var userUpdateDto = new UpdateUserDto();
        patchDoc.ApplyTo(userUpdateDto, ModelState);
        if (!TryValidateModel(userUpdateDto))
            return UnprocessableEntity(ModelState);
        
        var currentUser = userRepository.FindById(userId);
        if (currentUser == null)
            return NotFound();
        mapper.Map(userUpdateDto, currentUser);
        userRepository.Update(currentUser);
        return NoContent();
    }

    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    public ActionResult<IEnumerable<UserDto>> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        pageNumber = Math.Max(MinPageNumber, pageNumber);
        pageSize = Math.Clamp(pageSize, MinPageSize, MaxPageSize);

        var page = userRepository.GetPage(pageNumber, pageSize);
        var previousPageNumber = Math.Max(MinPageNumber, Math.Min(pageNumber - 1, page.TotalPages));
        var pagination = new
        {
            previousPageLink = page.HasPrevious
                ? Url.Link(nameof(GetUsers), new { pageNumber = previousPageNumber, pageSize })
                : null,
            nextPageLink = page.HasNext
                ? Url.Link(nameof(GetUsers), new { pageNumber = pageNumber + 1, pageSize })
                : null,
            totalCount = page.TotalCount,
            pageSize = page.PageSize,
            currentPage = page.CurrentPage,
            totalPages = page.TotalPages
        };
        Response.Headers["X-Pagination"] = JsonConvert.SerializeObject(pagination);

        return Ok(mapper.Map<IEnumerable<UserDto>>(page));
    }

    [HttpOptions]
    public IActionResult Options()
    {
        Response.Headers.Allow = "POST, GET, OPTIONS";
        return Ok();
    }
}
