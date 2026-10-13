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
            return NotFound();
        var userDto = mapper.Map<UserDto>(user);
        if (HttpMethods.IsHead(Request.Method))
        {
            Response.ContentType = "application/json; charset=utf-8";
            return Ok();
        }

        return Ok(userDto);
    }

    [HttpGet(Name = "GetUsers")]
    public ActionResult<IEnumerable<UserDto>> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1)
        {
            pageNumber = Math.Max(1, pageNumber);
        }

        if (pageSize is < 1 or > 20)
        {
            pageSize = Math.Clamp(pageSize, 1, 20);
        }

        var pageList = userRepository.GetPage(pageNumber, pageSize);

        var paginationHeader = new
        {
            previousPageLink = pageNumber == 1
                ? null
                : linkGenerator.GetUriByRouteValues(HttpContext, "GetUsers", new { pageNumber = pageNumber - 1, pageSize }),
            nextPageLink = pageList.CurrentPage >= pageList.TotalPages
                ? null
                : linkGenerator.GetUriByRouteValues(HttpContext, "GetUsers", new { pageNumber = pageNumber + 1, pageSize }),
            totalCount = pageList.TotalCount,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages,
        };

        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

        var users = mapper.Map<IEnumerable<UserDto>>(pageList);
        return Ok(users);
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] CreateUserDto dto)
    {
        if (dto.Login != null && !dto.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError(
                "Login",
                "Login must consist of letters or digits");
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var user = mapper.Map<UserEntity>(dto);

        var createdUser = userRepository.Insert(user);

        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUser.Id },
            createdUser.Id);
    }

    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromRoute] string userId, [FromBody] UpdateUserDto dto)
    {
        if (!Guid.TryParse(userId, out var id) || dto == null)
            return BadRequest();

        var user = userRepository.FindById(id);

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        if (user == null)
        {
            user = mapper.Map(dto, new UserEntity(id));
        }
        else
        {
            mapper.Map(dto, user);
        }

        userRepository.UpdateOrInsert(user, out var isInserted);
        if (isInserted)
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId },
                id
            );
        return NoContent();
    }

    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser([FromRoute] string userId, [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc)
    {
        if (!Guid.TryParse(userId, out var id))
            return NotFound();

        if (patchDoc == null)
            return BadRequest();

        var user = userRepository.FindById(id);
        if (user == null)
        {
            return NotFound();
        }

        var dto = mapper.Map<UpdateUserDto>(user);

        patchDoc.ApplyTo(dto, ModelState);

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        if (!TryValidateModel(dto))
        {
            return UnprocessableEntity(ModelState);
        }

        mapper.Map(dto, user);
        userRepository.Update(user);
        return NoContent();
    }

    [HttpDelete("{userId:guid}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();

        userRepository.Delete(userId);
        return NoContent();
    }

    [HttpOptions]
    public IActionResult GetOptions()
    {
        Response.Headers.Allow = "GET, POST, OPTIONS";
        return Ok();
    }
}