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
    private readonly IUserRepository repository;
    private readonly IMapper mapper;
    private readonly LinkGenerator linkGenerator;
    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        repository = userRepository;
        this.mapper = mapper;
        this.linkGenerator = linkGenerator;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var userEntity = repository.FindById(userId);
        if (userEntity == null)
            return NotFound();
        var userDto = mapper.Map<UserDto>(userEntity);
        if (!HttpMethods.IsHead(Request.Method)) return Ok(userDto);
        Response.ContentType = "application/json; charset=utf-8";
        return Ok();

    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] UserCreateDto userDto)
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

    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UserPutDto userDto)
    {
        if (userDto == null || userId == Guid.Empty)
            return BadRequest();
        if (!ModelState.IsValid)
        {
            ModelState.AddModelError("Login", "Invalid login");
            return UnprocessableEntity(ModelState);
        }
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
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserPutDto> patchDoc)
    {
        var userEntity = repository.FindById(userId);
        if (patchDoc == null)
            return BadRequest();
        if (userId == Guid.Empty || userEntity == null)
            return NotFound();
        var userUpdate = new UserPutDto();
        patchDoc.ApplyTo(userUpdate, ModelState);
        if (!TryValidateModel(userUpdate))
            return UnprocessableEntity(ModelState);
        var user = mapper.Map(userUpdate, userEntity);
        repository.Update(user);
        return NoContent();
    }
    
    [HttpDelete("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId)
    {
        var userEntity = repository.FindById(userId);
        if (userId == Guid.Empty || userEntity == null)
            return NotFound();
        repository.Delete(userId);
        return NoContent();
    }
    
    [HttpGet("")]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUser([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 1 : pageSize;
        pageSize = pageSize > 20 ? 20 : pageSize;
        var pageList = repository.GetPage(pageNumber, pageSize);
        var previousLink = pageList.HasPrevious ? linkGenerator.GetUriByRouteValues(HttpContext, "", new {pageNumber = pageNumber - 1, pageSize = pageSize}) : null;
        var nextLink = pageList.HasNext ? linkGenerator.GetUriByRouteValues(HttpContext, "", new {pageNumber = pageNumber + 1, pageSize = pageSize}) : null;

        var paginationHeader = new
        {
            previousPageLink = previousLink,
            nextPageLink = nextLink,
            totalCount = pageList.TotalCount,
            pageSize = pageSize,
            currentPage = pageNumber,
            totalPages = pageList.TotalPages,
        };
        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        return Ok(pageList);
    }
}