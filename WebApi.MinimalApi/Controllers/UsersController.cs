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

    public UsersController(IUserRepository userRepository, IMapper mapper,  LinkGenerator linkGenerator)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
        this.linkGenerator = linkGenerator;
    }

    [HttpGet("{userId}", Name=nameof(GetUserById))]
    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    public ActionResult GetUserById([FromRoute] Guid userId)
    {    
        if HttpMethods.IsHead(Request.Method)
            Response.Body = Stream.Null;
        var user = userRepository.FindById(userId);
        if (user == null)
        {
            return NotFound();
        }

        var dto = mapper.Map<UserDto>(user);

        return Ok(dto);
    }
    
    [HttpGet(Name = "GetUsers")]
    [Produces("application/json", "application/xml")]
    public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 20) pageSize = 20;

        var pageList = userRepository.GetPage(pageNumber, pageSize);

        var users = mapper.Map<IEnumerable<UserDto>>(pageList);

        var totalCount = pageList.TotalCount;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var previousPageLink = pageNumber > 1
            ? linkGenerator.GetUriByRouteValues(
                HttpContext,
                "GetUsers",
                new { pageNumber = pageNumber - 1, pageSize })
            : null;

        var nextPageLink = pageNumber < totalPages
            ? linkGenerator.GetUriByRouteValues(
                HttpContext,
                "GetUsers",
                new { pageNumber = pageNumber + 1, pageSize })
            : null;

        var paginationHeader = new
        {
            previousPageLink,
            nextPageLink,
            totalCount,
            pageSize,
            currentPage = pageNumber,
            totalPages
        };

        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

        return Ok(users);
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] CreateUserDto? user)
    {
        if (user == null)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        if (!user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("Login", "Login should be alphanumeric");
            return UnprocessableEntity(ModelState);
        }
        
        var userEntity = mapper.Map<UserEntity>(user);
        var createdEntity = userRepository.Insert(userEntity);
        return CreatedAtRoute(nameof(GetUserById), new{userId=createdEntity.Id}, createdEntity.Id);
    }

    
    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromBody] UpdateUserDto? user, [FromRoute] Guid? userId)
    {
        if (user == null)
            return BadRequest();
        if (userId is null)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        user.Id = userId;
        
        var userEntity = mapper.Map<UserEntity>(user);
        userRepository.UpdateOrInsert(userEntity, out var inserted);
        if (inserted)
            return CreatedAtRoute(nameof(GetUserById), new{userId=userEntity.Id}, userEntity.Id);
        return NoContent();
        
    }
    
    [HttpPatch("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser(
        Guid? userId, 
        [FromBody] JsonPatchDocument<UpdateUserDto>? patchDoc)
    {
        if (userId is null)
            return NotFound();
        if (patchDoc is null)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = userRepository.FindById(userId.Value);
        if (userEntity == null)
            return NotFound();

        var userToPatch = mapper.Map<UpdateUserDto>(userEntity);

        patchDoc.ApplyTo(userToPatch, ModelState);

        if (!TryValidateModel(userToPatch))
            return UnprocessableEntity(ModelState);

        mapper.Map(userToPatch, userEntity);
        userRepository.Update(userEntity);

        return NoContent();
    }
    
    [HttpDelete("{userId}")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(204, "Пользователь удален")]
    [SwaggerResponse(404, "Пользователь не найден")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (userId == Guid.Empty || userRepository.FindById(userId) == null)
            return NotFound();

        userRepository.Delete(userId);
        return NoContent();
    }

    [HttpOptions]
    [SwaggerResponse(200, "OK")]
    public IActionResult Options()
    {
        Response.Headers.Append("Allow", new[] {"GET", "POST", "OPTIONS"});
        return Ok();
    }    
}
