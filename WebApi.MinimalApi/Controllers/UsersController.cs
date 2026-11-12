using System.Net;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[Produces("application/json", "application/xml")]
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

    [HttpHead("{userId}")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        if (HttpMethods.IsHead(Request.Method))
        {
            Response.Body = Stream.Null;
        }

        var user = userRepository.FindById(userId);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(mapper.Map<UserDto>(user));
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] CreateUserDto userDto)
    {
        if (userDto is null)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        if (!userDto.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("Login", "Login should contain only letters or digits");
        }
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }
        var userEntity = mapper.Map<UserEntity>(userDto);

        userEntity = userRepository.Insert(userEntity);

        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = userEntity.Id },
            userEntity.Id);
    }

    [HttpPut("{userId}")]
    public IActionResult UpsertUserById([FromRoute] Guid userId, [FromBody] UpdateUserDto userDto)
    {
        if (userDto is null || userId == Guid.Empty)
        {
            return BadRequest();
        }

        userDto.Id = userId;
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }
        var userEntity = mapper.Map<UserEntity>(userDto);

        userRepository.UpdateOrInsert(userEntity, out var isInserted);
        if (isInserted)
        {
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id
            );
        }
        return NoContent();
    }

    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUserById([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc)
    {
        if (patchDoc is null)
        {
            return BadRequest();
        }
        if (userId == Guid.Empty)
        {
            return NotFound();
        }

        var existingUser = userRepository.FindById(userId);
        if (existingUser is null)
        {
            return NotFound();
        }

        var userDto = new UpdateUserDto()
        {
            Id = userId,
        };
        patchDoc.ApplyTo(userDto, ModelState);
        TryValidateModel(userDto);

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var userEntity = mapper.Map<UserEntity>(userDto);
        userRepository.Update(userEntity);
        return NoContent();
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUserbyId([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
        {
            return NotFound();
        }
        userRepository.Delete(userId);
        return NoContent();
    }

    [HttpGet(Name = nameof(GetAllUsers))]
    public IActionResult GetAllUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 20);

        var pageList = userRepository.GetPage(pageNumber, pageSize);

        var previousPageLink = pageList.HasPrevious ? linkGenerator.GetUriByRouteValues(
            HttpContext,
            nameof(GetAllUsers),
            new
            {
                pageNumber = pageList.CurrentPage - 1,
                pageSize = pageList.PageSize,
            }
        ) : null;
        var nextPageLink = pageList.HasNext ? linkGenerator.GetUriByRouteValues(
            HttpContext,
            nameof(GetAllUsers),
            new
            {
                pageNumber = pageList.CurrentPage + 1,
                pageSize = pageList.PageSize,
            }
        ) : null;
        var paginationHeader = new
        {
            previousPageLink = previousPageLink,
            nextPageLink = nextPageLink,
            totalCount = pageList.Count,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages,
        };
        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

        var users = mapper.Map<IEnumerable<UserDto>>(pageList);
        return Ok(users);
    }

    [HttpOptions]
    public IActionResult GetOptions()
    {
        Response.Headers.Add("Allow", new string[] { "GET", "POST", "OPTIONS" });
        return Ok();
    }
}
