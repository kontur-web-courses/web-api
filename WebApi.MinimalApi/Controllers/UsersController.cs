using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
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

    [HttpGet("{userId:guid}", Name = nameof(GetUserById))]
    [HttpHead("{userId:guid}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();

        if (!HttpMethods.IsHead(Request.Method)) return Ok(mapper.Map<UserDto>(user));
        var acceptHeader = Request.Headers.Accept.ToString();
        var contentType = acceptHeader.Contains("application/xml") 
            ? "application/xml; charset=utf-8" 
            : "application/json; charset=utf-8";

        Response.Headers.ContentType = contentType;
        return Ok();

    }

    [HttpPost(Name = nameof(CreateUser))]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] CreateUserDto? user)
    {
        if (user is null)
            return BadRequest();
        if(!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        if(!user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError(nameof(user.Login), $"Login must contain only letters and numbers.");
            return UnprocessableEntity(ModelState);
        }
        var entity = new UserEntity
        {
            Login = user.Login,
            FirstName = user.FirstName,
            LastName = user.LastName,
        };
        
        var createdUser = userRepository.Insert(entity);
        return new CreatedAtRouteResult(nameof(GetUserById), 
            new { userId = createdUser.Id }, createdUser.Id);
    }

    [HttpPut("{userId}", Name = nameof(UpdateUser))]
    [Consumes("application/json")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdateUserDto? user)
    {
        if (user == null || userId == Guid.Empty)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = mapper.Map(user, new UserEntity(userId));
        userRepository.UpdateOrInsert(userEntity, out var isInserted);

        if (isInserted)
        {
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id);
        }

        return NoContent();
    }
    
    [HttpPatch("{userId:guid}", Name = nameof(PartiallyUpdateUser))]
    [Consumes("application/json-patch+json")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserDto>? patchUser)
    {
        if (patchUser is null)
            return BadRequest();
        var emptyOp = patchUser.Operations.Where(op => string.IsNullOrEmpty(op.value.ToString()))
            .ToList();
        if (emptyOp.Count > 0)
        {
            foreach (var op in emptyOp)
            {
                ModelState.AddModelError(op.path, $"Patch operations must not be empty value from path {op.path}.");
            }
            return UnprocessableEntity(ModelState);
        }
        if(!patchUser.Operations.Where(op => op.path == "login")
               .All(op =>  (op.value.ToString() ?? throw new InvalidOperationException()).All(char.IsLetterOrDigit)))
        {
            ModelState.AddModelError("login", $"Login must contain only letters and numbers.");
            return UnprocessableEntity(ModelState);
        }
        if(!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        if (!TryValidateModel(patchUser))
        {
            ModelState.AddModelError(nameof(patchUser), $"UserDto can be contain only Login, FirstName and LastName");
            return UnprocessableEntity(ModelState);
        }

        var user = userRepository.FindById(userId);
        if (user is null)
        {
            return NotFound();
        }
        var updateUserDto = mapper.Map<UpdateUserDto>(user);
        
        patchUser.ApplyTo(updateUserDto, ModelState);
        var updateUser = mapper.Map<UserEntity>(updateUserDto);
        userRepository.Update(updateUser);
        return NoContent();
    }

    [HttpDelete("{userId:guid}", Name = nameof(DeleteUser))]
    [Consumes("application/json")]
    [Produces("application/json", "application/xml")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var user = userRepository.FindById(userId);
        if (user is null)
        {
            return NotFound();
        };
        userRepository.Delete(userId);
        return NoContent();
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
                ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber - 1, pageSize })
                : null,
            nextPageLink = pageList.HasNext
                ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber + 1, pageSize })
                : null,
            totalCount = pageList.TotalCount,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages,
        };
        Response.Headers["X-Pagination"] = JsonConvert.SerializeObject(paginationHeader);

        return Ok(users);
    }

    [HttpOptions]
    public IActionResult GetUsersOptions()
    {
        Response.Headers.Allow = "POST, GET, OPTIONS";
        return Ok();
    }
}
