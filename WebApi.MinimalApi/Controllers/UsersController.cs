using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class  UsersController : Controller
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

    
    [Produces("application/json", "application/xml")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [HttpHead("{userId}")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();

        if (!HttpMethods.IsHead(Request.Method)) return mapper.Map<UserDto>(user);
        Response.Headers.ContentType = "application/json; charset=utf-8";
        
        return Ok();
    }
    
    [Produces("application/json", "application/xml")]
    [HttpGet(Name = nameof(GetUsers))]
    public ActionResult<PageList<UserEntity>> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var pageNumberValue = pageNumber < 1 ? 1 : pageNumber;
        var pageSizeValue = Math.Clamp(pageSize, 1, 20);


        var pageList = userRepository.GetPage(pageNumberValue, pageSizeValue);

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
        
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);
        return Ok(users);
    }
    
    [Produces("application/json", "application/xml")]
    [HttpPost(Name = nameof(CreateUser))]
    public IActionResult CreateUser([FromBody] UserCreateDto? user)
    {
        if (user == null)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        var createdUserEntity = userRepository.Insert(mapper.Map<UserEntity>(user));

        var responseBody = Request.Headers.Accept.Any(h => h != null && h.Contains("xml"))
            ? (object)createdUserEntity.Id
            : new { id = createdUserEntity.Id };
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            responseBody);
    }

    [Produces("application/json", "application/xml")]
    [HttpPut(nameof(CreateUser)), Route("{userId}")]
    public IActionResult CreateUser([FromRoute] Guid userId, [FromBody] UserUpdateDto? user)
    {
        if (user == null || userId == Guid.Empty)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = new UserEntity(userId);
        
        mapper.Map(user, userEntity);
        
        userRepository.UpdateOrInsert(userEntity, out var inserted);
        
        if (!inserted)
            return NoContent();
        
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = userEntity.Id },
            userEntity.Id);
    }

    [Produces("application/json", "application/xml")]
    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser(
        [FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserUpdateDto>? patchDoc

    )
    {
        if (userId == Guid.Empty)
            return NotFound();
        
        if (patchDoc == null)
            return BadRequest();
        
        var userEntity = userRepository.FindById(userId);
        if (userEntity == null)
            return NotFound();
        
        var patchDto = mapper.Map<UserUpdateDto>(userEntity);
        
        patchDoc.ApplyTo(patchDto, ModelState);
        
        TryValidateModel(patchDto);
        
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        mapper.Map(patchDto, userEntity);
        
        userRepository.Update(userEntity);
        
        return NoContent();
    }

    [Produces("application/json", "application/xml")]
    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (userId == Guid.Empty)
            return NotFound();
        
        var userEntity = userRepository.FindById(userId);
        if (userEntity == null)
            return NotFound();
        
        userRepository.Delete(userId);

        return NoContent();
    }
    
    [HttpOptions]
    public IActionResult Options()
    {
        Response.Headers.Append("Allow", "POST, GET, OPTIONS");
        return Ok();
    }
}