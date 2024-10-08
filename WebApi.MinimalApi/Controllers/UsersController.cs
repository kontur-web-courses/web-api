using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

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

    [HttpHead("{userId}")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        if (Request.Method == "HEAD")
            Response.Body = Stream.Null;
        
        var user = userRepository.FindById(userId);
        
        if (user == null)
            return NotFound();
        
        return Ok(mapper.Map<UserDto>(user));
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] CreatedUserDto createdUser)
    {
        if (createdUser == null)
            return BadRequest();
        
        var createdUserEntity = mapper.Map<UserEntity>(createdUser);
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        if (!createdUserEntity.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError("Login", "Login should contain only letters or digits");
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        createdUserEntity = userRepository.Insert(createdUserEntity);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdatedUserDto updatedUser)
    {
        if (updatedUser == null || userId == Guid.Empty)
            return BadRequest();
        
        updatedUser.Id = userId;
        var updatedUserEntity = mapper.Map<UserEntity>(updatedUser);
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        userRepository.UpdateOrInsert(updatedUserEntity, out var isInserted);
        if (isInserted)
            return Created("User not found in repository. Created a new one successfully", updatedUserEntity);
        return NoContent();
    }

    [HttpPatch("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdatedUserDto> patchDocument)
    {
        if (patchDocument == null)
            return BadRequest();
        if (userId == Guid.Empty || userRepository.FindById(userId) == null)
            return NotFound();

        var updatedUser = new UpdatedUserDto()
        {
            Id = userId,
        };
        patchDocument.ApplyTo(updatedUser, ModelState);
        TryValidateModel(updatedUser);
        
        var updatedUserEntity = mapper.Map<UserEntity>(updatedUser);
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        userRepository.Update(updatedUserEntity);
        return NoContent();
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (userId == Guid.Empty || userRepository.FindById(userId) == null)
            return NotFound();
        
        userRepository.Delete(userId);
        return NoContent();
    }

    [HttpGet]
    [Produces("application/json", "application/xml")]
    public IActionResult GetUsers([FromQuery] int pageNumber=1, [FromQuery] int pageSize=10)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 20);
        
        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var previousPageLink = pageList.HasPrevious
            ? linkGenerator.GetUriByRouteValues(HttpContext, "",
                new
                {
                    pageNumber = pageList.CurrentPage - 1,
                    pageSize = pageList.PageSize
                })
            : null;
        var nextPageLink = pageList.HasNext
            ? linkGenerator.GetUriByRouteValues(HttpContext, "", 
                new 
                {
                    pageNumber = pageList.CurrentPage + 1, 
                    pageSize = pageList.PageSize 
                }) 
            : null;
        
        var paginationHeader = new
        {
            previousPageLink,
            nextPageLink,
            totalCount = pageList.Count,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages,
        };
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);
        return Ok(users);
    }

    [HttpOptions]
    public IActionResult Options()
    {
        Response.Headers.Append("Allow", new[] {"GET", "POST", "OPTIONS"});
        return Ok();
    }
}