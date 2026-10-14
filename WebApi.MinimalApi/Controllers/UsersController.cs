using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
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
    
    public UsersController(IUserRepository userRepository, IMapper mapper,  LinkGenerator linkGenerator)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
        this.linkGenerator = linkGenerator;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();
        
        if (HttpMethods.IsHead(Request.Method))
        {
            Response.ContentType = "application/json; charset=utf-8";
            return Ok();
        }
        return Ok(mapper.Map<UserDto>(user));
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] CreateUserDto user)
    {
        if (user == null)
            return BadRequest();
        if (user.Login != null && !user.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError("Login", "Login should contain only letters or digits");
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = mapper.Map<UserEntity>(user);
        var createdUser = userRepository.Insert(userEntity);
        
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUser.Id },
            createdUser.Id);
    }

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdateUserDto user)
    {
        if (user == null || userId == Guid.Empty)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = mapper.Map(user, new UserEntity(userId));
        userRepository.UpdateOrInsert(userEntity, out var isInserted);

        if (isInserted)
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id);

        return NoContent();
    }
    [HttpPatch("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser(
        [FromRoute] Guid userId, 
        [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc)
    {
        if (patchDoc == null)
            return BadRequest();
        
        var userEntity = userRepository.FindById(userId);
        if (userEntity == null)
            return NotFound();
        
        var updateDto = mapper.Map<UpdateUserDto>(userEntity);
        
        patchDoc.ApplyTo(updateDto, ModelState);
        
        TryValidateModel(updateDto);
        
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        mapper.Map(updateDto, userEntity);
        
        userRepository.UpdateOrInsert(userEntity, out var isInserted);
        
        return NoContent();

    }

    [HttpDelete("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (userId == Guid.Empty)
            return NotFound();
        if(userRepository.FindById(userId) == null)
            return NotFound();
        userRepository.Delete(userId);
        return NoContent();
    }
    
    
    [HttpOptions]
    [Route("")]
    public IActionResult GetOptions()
    {
        Response.Headers.Add("Allow", "GET, POST, OPTIONS");
        return Ok();
    }

    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    public IActionResult GetUsers([FromQuery] int pageNumber = 1,  [FromQuery] int pageSize = 10)
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
        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        return Ok(users);
    }
}