using System.Xml.XPath;
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
    private readonly IUserRepository repository;
    private readonly IMapper mapper;
    private readonly LinkGenerator linkGenerator;
    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        repository = userRepository;
        this.mapper = mapper;
        this.linkGenerator  = linkGenerator;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var userEntity = repository.FindById(userId);
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
    public IActionResult CreateUser([FromBody] PostUserDto userDto)
    {
        if (userDto == null)
            return BadRequest();
        if (!ModelState.IsValid || !userDto.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("Login", "Invalid login");
            return UnprocessableEntity(ModelState);
        }
        var user = repository.Insert(mapper.Map<UserEntity>(userDto));
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId  = user.Id },
            user.Id);
    }
    
    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] PutUserDto userDto)
    {
        if (userDto == null || userId == Guid.Empty)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var userEntity = repository.FindById(userId) ?? new UserEntity(userId);
        var user = mapper.Map(userDto, userEntity);
        repository.UpdateOrInsert(user, out var isInserted);
        if (isInserted)
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = user.Id },
                user.Id);
        return NoContent();
    }
    
    [HttpPatch("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<PutUserDto> patchDoc)
    {
        var userEntity = repository.FindById(userId);
        if (patchDoc == null)
            return BadRequest();
        if (userId == Guid.Empty || userEntity == null)
            return NotFound();
        var userUpdate = new PutUserDto();
        patchDoc.ApplyTo(userUpdate, ModelState);
        if (!TryValidateModel(userUpdate))
            return UnprocessableEntity(ModelState);
        var user = mapper.Map(userUpdate, userEntity);
        repository.Update(user);
        return NoContent();
    }

    [HttpDelete("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var userEntity = repository.FindById(userId);
        if (userEntity is null)
            return NotFound();
        repository.Delete(userId);
        return NoContent();
    }
    
    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    public ActionResult<IEnumerable<UserDto>> GetUsers([FromQuery] int pageSize = 10, [FromQuery] int pageNumber = 1)
    {
        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = pageSize < 1 ? 1 : (pageSize > 20 ? 20 : pageSize);
        var pageList = repository.GetPage(pageNumber, pageSize);
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);
        var previousPageLink = pageList.HasPrevious
            ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber - 1, pageSize })
            : null;
        var nextPageLink = pageList.HasNext
            ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber + 1, pageSize })
            : null;
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

        return Ok(users);
    }
    
    [HttpOptions]
    public IActionResult GetUserOptions()
    {
        Response.Headers.Add("Allow", "POST, GET, OPTIONS");
        return Ok();
    }
}