using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;

    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
    }

    [HttpGet("{userId}")]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();

        var dto = mapper.Map<UserDto>(user);
        return Ok(dto);
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] object user)
    {
        throw new NotImplementedException();
    }

    [HttpGet("users")]
    [Produces("application/json", "application/xml")]
    public ActionResult<IEnumerable<UserDto>> GetUsers([FromQuery] int? pageNumber, [FromQuery] int? pageSize)
    {
        var number = Math.Max(pageNumber ?? 1, 1);
        var size = Math.Clamp(pageSize ?? 10, 1, 20);

        var page = userRepository.GetPage(number, size);
        var users = mapper.Map<IEnumerable<UserDto>>(page);

        var pagination = new
        {
            previousPageLink = page.HasPrevious
                ? Url.Link(nameof(GetUsers), new { pageNumber = number - 1, pageSize = size })
                : null,
            nextPageLink = page.HasNext
                ? Url.Link(nameof(GetUsers), new { pageNumber = number + 1, pageSize = size })
                : null,
            totalCount = page.TotalCount,
            pageSize = page.PageSize,
            currentPage = page.CurrentPage,
            totalPages = page.TotalPages
        };
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(pagination));

        return Ok(users);
    }
    
    [HttpOptions]
    public IActionResult GetUsersOptions()
    {
        Response.Headers.Append("Allow", "GET, POST, OPTIONS");
        return Ok();
    }
}