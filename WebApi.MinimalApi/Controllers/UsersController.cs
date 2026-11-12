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
    
    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
    }

    [HttpGet("{userId:guid}", Name = nameof(GetUserById))]
    [HttpHead("{userId:guid}")]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);

        if (user is null)
            return NotFound(user);

        if (!HttpMethods.IsHead(Request.Method)) 
            return Ok(mapper.Map<UserDto>(user));
        
        Response.ContentType = "application/json; charset=utf-8";
        return Ok();

    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] UserCreateDto? user)
    {
        if (user is null)
            return BadRequest();

        if (string.IsNullOrWhiteSpace(user.Login))
        {
            ModelState.AddModelError("login", "Login is necessary");
            return UnprocessableEntity(ModelState);
        }

        if (!user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("login", "Login is invalid");
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
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromRoute] string userId, [FromBody] UserUpdateDto? user)
    {
        if (!Guid.TryParse(userId, out var id) || user is null)
            return BadRequest();

        if (!TryValidateModel(user))
            return UnprocessableEntity(ModelState);
       
        if (string.IsNullOrEmpty(user.FirstName))
        {
            ModelState.AddModelError("firstName", "FirstName is required");
            return UnprocessableEntity(ModelState);
        }
        if (string.IsNullOrEmpty(user.LastName))
        {
            ModelState.AddModelError("lastName", "LastName is required");
            return UnprocessableEntity(ModelState);
        }
        
        var entity = new UserEntity(id)
        {
            Login = user.Login,
            FirstName = user.FirstName,
            LastName = user.LastName,
        };
        
        userRepository.UpdateOrInsert(entity, out var isInserted);
        return isInserted 
            ? CreatedAtRoute(nameof(GetUserById), new { userId = entity.Id }, entity.Id) 
            : NoContent();
    }
    
    [HttpPatch("{userId:guid}", Name = nameof(PartiallyUpdateUser))]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserPatchDto>? patchDoc)
    {
        if (patchDoc is null)
            return BadRequest();
        if (userId == Guid.Empty)
            return NotFound();

        var userEntity = userRepository.FindById(userId);
        if (userEntity is null)
            return NotFound();

        var userPatch = new UserPatchDto
        {
            Login = userEntity.Login,
            FirstName = userEntity.FirstName,
            LastName = userEntity.LastName
        };

        patchDoc.ApplyTo(userPatch, ModelState);

        if (string.IsNullOrEmpty(userPatch.Login))
            ModelState.AddModelError("Login", "Login is required");
        else if (!userPatch.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError("Login", "Login must contain digits and letters");

        if (string.IsNullOrEmpty(userPatch.FirstName))
        {
            ModelState.AddModelError("firstName", "FirstName is required");
            return UnprocessableEntity(ModelState);
        }
        if (string.IsNullOrEmpty(userPatch.LastName))
        {
            ModelState.AddModelError("lastName", "LastName is required");
            return UnprocessableEntity(ModelState);
        }

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        if (userPatch.Login != null)
            userEntity.Login = userPatch.Login;
        if (userPatch.FirstName != null)
            userEntity.FirstName = userPatch.FirstName;
        if (userPatch.LastName != null)
            userEntity.LastName = userPatch.LastName;

        userRepository.Update(userEntity);
        return NoContent();
    }

    [HttpDelete("{userId:guid}", Name = nameof(DeleteUser))]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (userId == Guid.Empty)
            return NotFound();
        
        var userEntity = userRepository.FindById(userId);
        if (userEntity is null)
            return NotFound();
        
        userRepository.Delete(userId);
        return NoContent();
    }
    
    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    public ActionResult<IEnumerable<UserDto>> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 20);

        var pageList = userRepository.GetPage(pageNumber, pageSize);

        string? previousPageLink = null;
        string? nextPageLink = null;

        if (pageList.HasPrevious)
            previousPageLink = Url.RouteUrl(nameof(GetUsers), new { pageNumber = pageNumber - 1, pageSize }, Request.Scheme);
        if (pageList.HasNext)
            nextPageLink = Url.RouteUrl(nameof(GetUsers), new { pageNumber = pageNumber + 1, pageSize }, Request.Scheme);

        var paginationHeader = new
        {
            previousPageLink,
            nextPageLink,
            totalCount = pageList.TotalCount,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages,
        };
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

        var users = mapper.Map<IEnumerable<UserDto>>(pageList);
        return Ok(users);
    }
    
    [HttpOptions(Name = nameof(GetUsersOptions))]
    public IActionResult GetUsersOptions()
    {
        var allowedMethods = new List<string>
        {
            HttpMethods.Get,
            HttpMethods.Post,
            HttpMethods.Options,
        };

        Response.Headers.Append("Allow", string.Join(", ", allowedMethods));
        return Ok();
    }
}