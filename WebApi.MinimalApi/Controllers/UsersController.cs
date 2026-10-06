using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
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

    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var userEntity = userRepository.FindById(userId);
        if (userEntity == null)
            return NotFound();
        
        if (HttpMethods.IsHead(Request.Method))
        {
            Response.ContentType = "application/json; charset=utf-8";
            return Ok();
        }
    
        var userDto = mapper.Map<UserDto>(userEntity);
        return Ok(userDto);
    }

    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] UserPostDto user)
    {
        if (user == null)
            return BadRequest();

        if (user.Login != null)
        {
            if (!user.Login.All(char.IsLetterOrDigit))
                ModelState.AddModelError("login", "login must be alphanumeric");
        }
       

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var userEntity = mapper.Map<UserEntity>(user);
        userEntity = userRepository.Insert(userEntity);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = userEntity.Id },
            userEntity.Id);
    }

    [HttpPut("{userId}")]
    [Consumes("application/json")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UserPutDto user)
    {
        if (user == null || userId == Guid.Empty)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var userEntity = mapper.Map(user, new UserEntity(userId));
        userRepository.UpdateOrInsert(userEntity, out var isInserted);
        if (isInserted)
            return CreatedAtAction(
                actionName: nameof(GetUserById),
                routeValues: new { userId },
                value: userId);

        return NoContent();
    }
    
    [HttpPatch("{userId}", Name = nameof(PatchUser))]
    [Consumes("application/json")]
    [Produces("application/json", "application/xml")]
    public IActionResult PatchUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserPatchDto>? patchDoc)
    {
        if (patchDoc is null)
            return BadRequest();
        if (userId == Guid.Empty)
            return NotFound();
        var userEntity = userRepository.FindById(userId);
        if (userEntity is null)
            return NotFound();
        var userToPatch = mapper.Map<UserPatchDto>(userEntity);
        patchDoc.ApplyTo(userToPatch, ModelState);

        if (!TryValidateModel(userToPatch) || !ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        mapper.Map(userToPatch, userEntity);
        userRepository.Update(userEntity);

        return NoContent();
    }

    [HttpDelete("{userId}", Name = nameof(DeleteUser))]
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
    public ActionResult<IEnumerable<UserDto>> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 20);
        
        var pageList = userRepository.GetPage(pageNumber, pageSize);
        
        string previousPageLink = null;
        string nextPageLink = null;

        if (pageList.HasPrevious)
        {
            previousPageLink = Url.RouteUrl(nameof(GetUsers), new { pageNumber = pageNumber - 1, pageSize }, Request.Scheme);
        }

        if (pageList.HasNext)
        {
            nextPageLink = Url.RouteUrl(nameof(GetUsers), new { pageNumber = pageNumber + 1, pageSize }, Request.Scheme);
        }
        
        var paginationHeader = new
        {
            previousPageLink,
            nextPageLink,
            totalCount = pageList.TotalCount,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages
        };

        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        
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
            HttpMethods.Options
        };

        Response.Headers.Add("Allow", string.Join(", ", allowedMethods));
    
        return Ok();
    }
}