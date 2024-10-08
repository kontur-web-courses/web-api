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
    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    readonly private IUserRepository userRepository;
    readonly private IMapper iMapper;
    readonly private LinkGenerator linkGenerator;
    public UsersController(IUserRepository userRepository, IMapper iMapper, LinkGenerator linkGenerator)
    {
        this.userRepository = userRepository;
        this.iMapper = iMapper;
        this.linkGenerator = linkGenerator;
    }

    [HttpHead("{userId}")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
        {
            return NotFound();
        }

        if (Request.HttpContext.Request.Method != "HEAD")
        {
            return Ok(iMapper.Map<UserDto>(user));
        }

        Response.ContentLength = 0;
        Response.ContentType = "application/json; charset=utf-8";
        return Ok();
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] PostUserDto? user)
    {
        if (user is null)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        if (user.Login.Any(x => !char.IsLetterOrDigit(x)))
        {
            ModelState.AddModelError("login", "Should contain letters and numbers only");
            return UnprocessableEntity(ModelState);
        }
        
        var createdUserEntity = userRepository.Insert(iMapper.Map<UserEntity>(user));
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdateUserDto? user)
    {
        if (user is null)
        {
            return BadRequest();
        }

        if (userId == Guid.Empty)
        {
            return BadRequest();
        }
        
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        userRepository.UpdateOrInsert(iMapper.Map(user, new UserEntity(userId)), out var updatedUserEntity);
        if (updatedUserEntity)
        {
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId },
                userId);
        }

        return NoContent();
    }

    [HttpPatch("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserDto>? patchDoc)
    {
        if (patchDoc is null)
        {
            return BadRequest();
        }
        
        var user = new UpdateUserDto();
        patchDoc.ApplyTo(user, ModelState);
        if (userId == Guid.Empty)
        {
            return NotFound();
        }
        
        if (!TryValidateModel(user))
        {
            return UnprocessableEntity(ModelState);
        }
        
        var currentUser = userRepository.FindById(userId);
        if (currentUser is null)
        {
            return NotFound();
        }
        
        userRepository.Update(iMapper.Map(user, new UserEntity(userId)));
        return NoContent();
    }

    [HttpDelete("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var currUser = userRepository.FindById(userId);
        if (currUser is null)
        {
            return NotFound();
        }
        
        userRepository.Delete(userId);
        return NoContent();
    }

    [HttpGet(Name = nameof(GetAllUsers))]
    [Produces("application/json", "application/xml")]
    public IActionResult GetAllUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var realPageSize = pageSize > 20 ? 20 : pageSize;
        var page = userRepository.GetPage(pageNumber, realPageSize);
        
        var paginationHeader = new 
        {
            currentPage = page.CurrentPage == 0 ? 1 : page.CurrentPage,
            totalPages = page.TotalPages,
            pageSize = page.PageSize == 0 ? 1 : page.PageSize,
            totalCount = page.TotalCount,
            nextPageLink = page.HasNext
                ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetAllUsers), new { pageNumber = pageNumber + 1, pageSize } )
                : null,
            previousPageLink = page.HasPrevious
                ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetAllUsers), new { pageNumber = pageNumber - 1, pageSize } )
                : null,
        };
        
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        return Ok(iMapper.Map<IEnumerable<UserDto>>(page));
    }

    [HttpOptions]
    [Produces("application/json", "application/xml")]
    public IActionResult Options()
    {
        Response.Headers.Append("Allow", new[] { "GET", "POST", "OPTIONS" });
        Response.ContentLength = 0;
        return Ok();
    }
}