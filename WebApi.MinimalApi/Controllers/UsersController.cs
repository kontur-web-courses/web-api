using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json", "application/xml")]
public class UsersController : Controller
{
    private readonly IMapper mapper;
    private readonly IUserRepository userRepository;
    private readonly LinkGenerator linkGenerator;
    
    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
        this.linkGenerator = linkGenerator;
    }
    
    [HttpGet("{userId:guid}", Name = nameof(GetUserById))]
    [HttpHead("{userId:guid}")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();

        if (HttpMethods.IsHead(Request.Method))
        {
            Response.Headers.ContentType = "application/json; charset=utf-8";
            return Ok();
        }

        var userDto = mapper.Map<UserDto>(user);
        return Ok(userDto);
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] UserToPostDto? user)
    {
        if (user == null)
            return BadRequest();
        if (string.IsNullOrEmpty(user.Login) || !user.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError(nameof(user.Login), "Unallowed chars in Login");
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        var userToPost = mapper.Map<UserEntity>(user);
        
        var value =  userRepository.Insert(userToPost);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = userToPost.Id },
            value.Id);
    }
    
    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UserUpdateDto? userUpdateDto)
    {
        if (userUpdateDto == null || userId == Guid.Empty)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var updatedUser = mapper.Map(userUpdateDto, new UserEntity(userId));

        userRepository.UpdateOrInsert(updatedUser, out var isNewUser);
    
        if (isNewUser)
        {
            return CreatedAtAction(
                actionName: nameof(GetUserById),
                routeValues: new { userId = updatedUser.Id },
                value: updatedUser.Id);
        }

        return NoContent();
    }

    [HttpPatch("{userId:guid}")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId,
        [FromBody] JsonPatchDocument<UserUpdateDto>? patchDoc)
    {
        if (patchDoc == null)
            return BadRequest();
        
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();
        
        var userToPatch = mapper.Map<UserUpdateDto>(user);
        
        patchDoc.ApplyTo(userToPatch, ModelState);
        
        TryValidateModel(userToPatch);
        
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        mapper.Map(userToPatch, user);
        userRepository.Update(user);
        
        return NoContent();
    }

    [HttpDelete("{userId:guid}")]
    public IActionResult RemoveUser([FromRoute] Guid userId)
    {
        var userEntity = userRepository.FindById(userId);
        if (userEntity == null)
            return NotFound();

        userRepository.Delete(userId);
        return NoContent();
    }

    [HttpGet(Name = "GetUsers")]
    public IActionResult GetUsers([FromQuery] int? pageNumber, [FromQuery] int? pageSize)
    {
        var page = pageNumber.GetValueOrDefault(1);
        var size = pageSize.GetValueOrDefault(10);

        if (page < 1)
            page = 1;
        if (size < 1)
            size = 1;
        if (size > 20)
            size = 20;
        
        var pageList = userRepository.GetPage(page, size);
        
        var prev = pageList.HasPrevious ? linkGenerator.GetUriByRouteValues(HttpContext, "GetUsers", new {pageNumber = page - 1, pageSize = size}) : null;
        var next = pageList.HasNext ? linkGenerator.GetUriByRouteValues(HttpContext, "GetUsers", new {pageNumber = page + 1, pageSize = size}) : null;
        
        var paginationHeader = new
        {
            previousPageLink = prev,
            nextPageLink = next,
            totalCount = pageList.Count,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages,
        };
        Response.Headers["X-Pagination"] = JsonConvert.SerializeObject(paginationHeader);

        var users = mapper.Map<IEnumerable<UserDto>>(pageList);
        
        return Ok(users);
    }
    
    [HttpOptions]
    public IActionResult GetOptions()
    {
        Response.Headers.Append("Allow", "GET, POST, OPTIONS");
        return Ok();
    }
}