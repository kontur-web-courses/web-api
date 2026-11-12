using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

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

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(200, "OK", typeof(UserDto))]
    [SwaggerResponse(404, "Пользователь не найден")]
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
    [Consumes("application/json")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(201, "Пользователь создан")]
    [SwaggerResponse(400, "Некорректные входные данные")]
    public IActionResult CreateUser([FromBody] CreatedUserDto user)
    {
        if (user == null)
            return BadRequest();
        
        var createdUserEntity = mapper.Map<UserEntity>(user);
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
    [Consumes("application/json")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(201, "Пользователь создан")]
    [SwaggerResponse(400, "Некорректные входные данные")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdatedUserDto user)
    {
        if (user == null || userId == Guid.Empty)
            return BadRequest();
        
        user.Id = userId;
        var updatedUserEntity = mapper.Map<UserEntity>(user);
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        userRepository.UpdateOrInsert(updatedUserEntity, out var isInserted);
        if (isInserted)
            return Created("User not found in repository. Created a new one successfully", updatedUserEntity);
        return NoContent();
    }

    [HttpPatch("{userId}")]
    [Consumes("application/json-patch+json")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(204, "Пользователь обновлен")]
    [SwaggerResponse(404, "Пользователь не найден")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdatedUserDto> patchDoc)
    {
        if (patchDoc == null)
            return BadRequest();
        if (userId == Guid.Empty || userRepository.FindById(userId) == null)
            return NotFound();

        var updatedUser = new UpdatedUserDto()
        {
            Id = userId,
        };
        patchDoc.ApplyTo(updatedUser, ModelState);
        TryValidateModel(updatedUser);
        
        var updatedUserEntity = mapper.Map<UserEntity>(updatedUser);
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        userRepository.Update(updatedUserEntity);
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

    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
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
    [SwaggerResponse(200, "OK")]
    public IActionResult Options()
    {
        Response.Headers.Append("Allow", new[] {"GET", "POST", "OPTIONS"});
        return Ok();
    }
}
