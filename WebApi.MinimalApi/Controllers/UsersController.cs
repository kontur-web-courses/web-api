using System.ComponentModel.DataAnnotations;
using AutoMapper;
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
    [HttpHead("{userId:guid}")]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
        {
            return NotFound();
        }
        if (HttpMethods.IsHead(Request.Method))
        {
            return Content(string.Empty, "application/json; charset=utf-8");
        }

        return mapper.Map<UserDto>(user);
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] CreateUserDto? user)
    {
        if (user is null)
        {
            return BadRequest();
        }

        CheckLogin(user.Login);

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var createdUserEntity = mapper.Map<UserEntity>(user);
        var insertedUserEntity = userRepository.Insert(createdUserEntity);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = insertedUserEntity.Id },
            insertedUserEntity.Id);
    }

    [HttpDelete("{userId:guid}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (userRepository.FindById(userId) != null)
        {
            userRepository.Delete(userId);
            return NoContent();
        }

        return NotFound();
    }

    [HttpPut("{userId}")]
    public ActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdateUserDto? dto)
    {
        if (userId == Guid.Empty || dto is null)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var user = mapper.Map(dto, new UserEntity(userId));

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

    private void CheckLogin(string? login)
    {
        if (!string.IsNullOrEmpty(login) && !login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("Login", "Login can contain only letters and digits");
        }
    }

    [HttpGet(Name = nameof(GetAllUsers))]
    [Produces("application/json", "application/xml")]
    public IActionResult GetAllUsers([Range(0, int.MaxValue)][FromQuery] int pageNumber = 1, [Range(1, 20)][FromQuery] int pageSize = 10)
    {
        if (!ModelState.IsValid)
        {

        }
        if (pageNumber <= 0)
            pageNumber = 1;
        if (pageSize <= 0)
            pageSize = 1;
        if (pageSize > 20)
            pageSize = 20;
        
        var usersPage = userRepository.GetPage(pageNumber, pageSize);
        var users = mapper.Map<IEnumerable<UserDto>>(usersPage);
        
        var paginationHeader = new
        {
            previousPageLink = usersPage.HasPrevious ? linkGenerator
                .GetUriByRouteValues(HttpContext, "GetAllUsers", 
                    new { pageNumber = pageNumber - 1, pageSize = pageSize }) : null, 
            nextPageLink = usersPage.HasNext ? linkGenerator
                .GetUriByRouteValues(HttpContext, "GetAllUsers",
                    new { pageNumber = pageNumber + 1, pageSize = pageSize }) : null,
            totalCount = usersPage.TotalCount,
            pageSize = usersPage.PageSize,
            currentPage = usersPage.CurrentPage,
            totalPages = usersPage.TotalPages,
        };
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

        return Ok(users);
    }
}