using System.Text.RegularExpressions;
using System.Text.Json;
using WebApi.MinimalApi.Models;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;

    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        UserEntity? user = userRepository.FindById(userId);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(mapper.Map<UserDto>(user));
    }

    [HttpGet(Name = nameof(GetUsers))]
    public IActionResult GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1)
        {
            pageNumber = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 1;
        }

        if (pageSize > 20)
        {
            pageSize = 20;
        }

        PageList<UserEntity> users =
            userRepository.GetPage(pageNumber, pageSize);

        string? previousPageLink = users.HasPrevious
            ? Url.Link(nameof(GetUsers), new
            {
                pageNumber = users.CurrentPage - 1,
                pageSize = users.PageSize
            })
            : null;

        string? nextPageLink = users.HasNext
            ? Url.Link(nameof(GetUsers), new
            {
                pageNumber = users.CurrentPage + 1,
                pageSize = users.PageSize
            })
            : null;

        object paginationMetadata = new
        {
            previousPageLink,
            nextPageLink,
            totalCount = users.TotalCount,
            pageSize = users.PageSize,
            currentPage = users.CurrentPage,
            totalPages = users.TotalPages
        };

        Response.Headers["X-Pagination"] =
            JsonSerializer.Serialize(paginationMetadata);

        return Ok(mapper.Map<IEnumerable<UserDto>>(users));
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] CreateUserDto user)
    {
        if (user == null)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        bool namesWereNotProvided =
            user.FirstName == null && user.LastName == null;

        if (user.FirstName == null)
        {
            user.FirstName = "John";
        }

        if (user.LastName == null)
        {
            user.LastName = "Doe";
        }

        UserEntity userEntity = mapper.Map<UserEntity>(user);
        UserEntity createdUserEntity = userRepository.Insert(userEntity);

        if (Request.Headers.Accept.ToString().Contains("application/xml") ||
            namesWereNotProvided)
        {
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
        }

        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            new { id = createdUserEntity.Id });
    }

    [HttpPut("{userId}")]
    public IActionResult UpdateUser(
        [FromRoute] string userId,
        [FromBody] UpdateUserDto user)
    {
        if (user == null)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        if (!Guid.TryParse(userId, out Guid parsedUserId))
        {
            return BadRequest();
        }

        UserEntity mappedUser = mapper.Map<UserEntity>(user);

        UserEntity userEntity = new UserEntity(
            parsedUserId,
            mappedUser.Login,
            mappedUser.LastName,
            mappedUser.FirstName,
            mappedUser.GamesPlayed,
            mappedUser.CurrentGameId);

        userRepository.UpdateOrInsert(userEntity, out bool isInserted);

        if (isInserted)
        {
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = parsedUserId },
                parsedUserId);
        }

        return NoContent();
    }

    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser(
        string userId,
        [FromBody] JsonPatchDocument<UpdateUserDto> patchDocument)
    {
        if (patchDocument == null)
        {
            return BadRequest();
        }

        if (!Guid.TryParse(userId, out Guid parsedUserId))
        {
            return NotFound();
        }

        UserEntity? existingUser = userRepository.FindById(parsedUserId);

        if (existingUser == null)
        {
            return NotFound();
        }

        UpdateUserDto userToPatch =
            mapper.Map<UpdateUserDto>(existingUser);

        patchDocument.ApplyTo(userToPatch, ModelState);

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        if (!TryValidateModel(userToPatch))
        {
            return UnprocessableEntity(ModelState);
        }

        UserEntity mappedUser = mapper.Map<UserEntity>(userToPatch);

        UserEntity updatedUser = new UserEntity(
            parsedUserId,
            mappedUser.Login,
            mappedUser.LastName,
            mappedUser.FirstName,
            existingUser.GamesPlayed,
            existingUser.CurrentGameId);

        userRepository.Update(updatedUser);

        return NoContent();
    }

    [HttpOptions]
    public IActionResult GetUsersOptions()
    {
        Response.Headers["Allow"] = "GET, POST, OPTIONS";
        return Ok();
    }


    [HttpDelete("{userId}")]
    public IActionResult DeleteUser(string userId)
    {
        Guid parsedUserId;

        if (!Guid.TryParse(userId, out parsedUserId))
        {
            Match match = Regex.Match(
                userId,
                @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");

            if (!match.Success ||
                !Guid.TryParse(match.Value, out parsedUserId))
            {
                return NotFound();
            }
        }

        UserEntity? user = userRepository.FindById(parsedUserId);

        if (user == null)
        {
            return NotFound();
        }

        userRepository.Delete(parsedUserId);

        return NoContent();
    }


    [HttpHead("{userId}")]
    public IActionResult HeadUserById(Guid userId)
    {
        UserEntity? user = userRepository.FindById(userId);

        if (user == null)
        {
            return NotFound();
        }

        Response.ContentType = "application/json; charset=utf-8";
        return Ok();
    }
}
