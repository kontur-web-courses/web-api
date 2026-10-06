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

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
            return NotFound();
        
        return Ok(mapper.Map<UserDto>(user));
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] CreateUserDto? user)
    {
        if (user is null) 
            return BadRequest();

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        if (!user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("Login", "Login must contain letters or digits.");
            return UnprocessableEntity(ModelState);
        }
        
        var userEntity = mapper.Map<UserEntity>(user);
        var createdUserEntity = userRepository.Insert(userEntity);
        
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }

    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromBody] UpdateUserDto? user, [FromRoute] Guid userId)
    {
        if (user is null || userId == Guid.Empty)
        {
            return BadRequest();
        }
        
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var userEntity = mapper.Map<UserEntity>(user);
        userEntity = new UserEntity(
            userId,
            userEntity.Login,
            userEntity.LastName,
            userEntity.FirstName,
            userEntity.GamesPlayed,
            userEntity.CurrentGameId);
        
        userRepository.UpdateOrInsert(userEntity, out var isInserted);
        
        if (isInserted) 
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id);

        return NoContent();
    }

    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser(
        [FromBody] JsonPatchDocument<UpdateUserDto>? patchDoc,
        [FromRoute] Guid userId)
    {
        var userDto = new UpdateUserDto();
        if (patchDoc is null)
            return BadRequest();
        
        patchDoc.ApplyTo(userDto, ModelState);
        TryValidateModel(userDto);

        if (!ModelState.IsValid)
        {
            if (userId == Guid.Empty)
                return NotFound();
            return UnprocessableEntity(ModelState);
        }

        var user = userRepository.FindById(userId);
        if (user is null)
            return NotFound();

        userRepository.Update(user);
        return NoContent();
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (userId == Guid.Empty)
            return NotFound();
        
        var user = userRepository.FindById(userId);
        if (user is null)
            return NotFound();
        
        userRepository.Delete(userId);
        return NoContent();
    }

    [HttpHead("{userId}")]
    public IActionResult HeadUser([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
            return NotFound();

        Response.Headers.ContentType = "application/json; charset=utf-8";
        return Ok();
    }

    [HttpGet]
    public ActionResult<IEnumerable<UserDto>> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1)
            pageNumber = 1;

        if (pageSize < 1)
            pageSize = 1;

        if (pageSize > 20)
            pageSize = 20;

        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);

        var totalCount = pageList.TotalCount;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        string? previousPageLink;
        if (pageNumber == 1)
            previousPageLink = linkGenerator.GetUriByRouteValues(
                HttpContext,
                nameof(GetUsers),
                new { pageNumber = pageNumber - 1, pageSize });
        else
            previousPageLink = string.Empty;

        string? nextPageLink;
        if (pageNumber == 20)
            nextPageLink = linkGenerator.GetUriByRouteValues(
                HttpContext,
                nameof(GetUsers),
                new { pageNumber = pageNumber + 1, pageSize });
        else
            nextPageLink = string.Empty;

        var paginationHeader = new
        {
            PreviousPageLink = previousPageLink,
            NextPageLink = nextPageLink,
            TotalCount = totalCount,
            PageSize = pageSize,
            CurrentPage = pageNumber,
            TotalPages = totalPages
        };

        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        return Ok(users);
    }
    
    [HttpOptions]
    public IActionResult OptionsUsers()
    {
        Response.Headers.Append("Allow", "POST, GET, OPTIONS");
        return Ok();
    }
}