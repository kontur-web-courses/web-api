using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Routing;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
using AutoMapper;
using Newtonsoft.Json;

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

    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 20);

        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);

        var paginationHeader = new
        {
            previousPageLink = pageList.HasPrevious
                ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageList.CurrentPage - 1, pageSize = pageList.PageSize })
                : null,
            nextPageLink = pageList.HasNext
                ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageList.CurrentPage + 1, pageSize = pageList.PageSize })
                : null,
            totalCount = pageList.TotalCount,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages
        };

        Response.Headers["X-Pagination"] = JsonConvert.SerializeObject(paginationHeader);
        return Ok(users);
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var userEntity = userRepository.FindById(userId);
        if (userEntity == null)
            return NotFound();
        
        var userDto = mapper.Map<UserDto>(userEntity);
        return Ok(userDto);
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] AddUserDto? user)
    {
        if (user == null)
        {
            return BadRequest();
        }
        
        if (!string.IsNullOrEmpty(user.Login) && !user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("Login", "Login should contain only letters and digits");
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }
        
        var userEntity = mapper.Map<UserEntity>(user);
        var createdUserEntity = userRepository.Insert(userEntity);
        
        var userDto = mapper.Map<UserDto>(createdUserEntity);

        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            userDto);
    }
    
    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromRoute] string userId, [FromBody] UpdateUserDto? user)
    {
        if (user == null)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        if (!Guid.TryParse(userId, out var userGuid))
        {
            return BadRequest();
        }
        
        var userEntity = mapper.Map(user, new UserEntity(userGuid));

        userRepository.UpdateOrInsert(userEntity, out var isInserted);

        if (isInserted)
        {
            var userDto = mapper.Map<UserDto>(userEntity);

            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userDto);
        }

        return NoContent();
    }

    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser([FromRoute] string userId, [FromBody] JsonPatchDocument<UpdateUserDto>? patchDoc)
    {
        if (patchDoc == null)
            return BadRequest();
        
        if (!Guid.TryParse(userId, out var userGuid))
            return NotFound();
        
        var userEntity = userRepository.FindById(userGuid);
        if (userEntity == null)
            return NotFound();

        var updateDto = new UpdateUserDto
        {
            Login = userEntity.Login,
            FirstName = userEntity.FirstName,
            LastName = userEntity.LastName,
        };
        
        patchDoc.ApplyTo(updateDto, ModelState);
        if (ModelState.IsValid)
            TryValidateModel(updateDto);
        
        if (!ModelState.IsValid)
        {
            var errors = ModelState.ToDictionary(
                entry => char.ToLowerInvariant(entry.Key[0]) + entry.Key[1..],
                entry => entry.Value.Errors.Select(error => error.ErrorMessage).ToArray()
            );

            return UnprocessableEntity(errors);
        }
        
        mapper.Map(updateDto, userEntity);
        userRepository.Update(userEntity);
        
        return NoContent();
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return NotFound();
        
        var userEntity = userRepository.FindById(userGuid);
        if (userEntity == null)
            return NotFound();
        
        userRepository.Delete(userGuid);
        return NoContent();
    }

    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult HeadUserById([FromRoute] string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return NotFound();
        
        var userEntity = userRepository.FindById(userGuid);
        if (userEntity == null)
            return NotFound();
        
        var userDto = mapper.Map<UserDto>(userEntity);
        Response.Body = Stream.Null;
        
        return Ok(userDto);
    }

    [HttpOptions]
    public IActionResult GetUsersOptions()
    {
        Response.Headers.Allow = "GET, POST, OPTIONS";
        return Ok();
    }
}