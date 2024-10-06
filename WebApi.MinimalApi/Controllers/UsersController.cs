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
        if (Request.Method == "HEAD")
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
    public IActionResult CreateUser([FromBody] CreateUserDto user)
    {
        if (user is null)
        {
            return BadRequest();
        }

        var userEntity = mapper.Map<UserEntity>(user);
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        if (!userEntity.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("Login", "Login should contain only letters or digits");
        }
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        userEntity = userRepository.Insert(userEntity);

        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = userEntity.Id },
            userEntity.Id);
    }

    [HttpPut("{userId}")]
    public IActionResult UpsertUserById([FromRoute] Guid userId, [FromBody] UpdateUserDto user)
    {
        if (user is null || userId == Guid.Empty)
        {
            return BadRequest();
        }

        user.Id = userId;
        var userEntity = mapper.Map<UserEntity>(user);
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        userRepository.UpdateOrInsert(userEntity, out var isInserted);
        if (isInserted)
        {
            return Created("New user created successfully", userEntity);
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

        var user = new UpdateUserDto()
        {
            Id = userId,
        };
        patchDoc.ApplyTo(user);
        TryValidateModel(user);

        var userEntity = mapper.Map<UserEntity>(user);
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var existingUser = userRepository.FindById(userId);
        if (existingUser is null)
        {
            return NotFound();
        }
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

    [HttpGet]
    public IActionResult GetAllUsers([FromQuery] int? pageNumber, [FromQuery] int? pageSize)
    {
        pageNumber = pageNumber ?? 1;
        pageNumber = Math.Max((int)pageNumber, 1);
        pageSize = pageSize ?? 10;
        pageSize = Math.Max((int)pageSize, 1);
        pageSize = Math.Min((int)pageSize, 20);

        var pageList = userRepository.GetPage((int)pageNumber, (int)pageSize);
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);

        var previousPageLink = pageList.HasPrevious ? linkGenerator.GetUriByRouteValues(
            HttpContext,
            "",
            new
            {
                pageNumber = pageList.CurrentPage - 1,
                pageSize = pageList.PageSize,
            }
        ) : null;
        var nextPageLink = pageList.HasNext ? linkGenerator.GetUriByRouteValues(
            HttpContext,
            "",
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

        return Ok(users);
    }

    [HttpOptions]
    public IActionResult GetOptions()
    {
        Response.Headers.Add("Allow", new string[] { "GET", "POST", "OPTIONS" });
        return Ok();
    }
}
