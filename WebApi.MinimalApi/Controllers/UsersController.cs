using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/users")]
[ApiController]
public class UsersController : Controller
{
    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    private IUserRepository userRepository;
    private IMapper mapper;
    private LinkGenerator linkGenerator;
    
    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
        this.linkGenerator = linkGenerator;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [HttpHead("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
            return NotFound();
        var result = mapper.Map<UserDto>(user);
        
        //if (!HttpMethods.IsHead(Request.Method))
        if (HttpContext.Request.Method != "HEAD")
            return Ok(result);
        Response.ContentType = "application/json; charset=utf-8";
        return Ok();
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] PostUserDto user)
    {
        var createdUserEntity = mapper.Map<UserEntity>(user);
        if (createdUserEntity is null)
            return BadRequest();
        if(createdUserEntity.Login?.All(char.IsLetterOrDigit) == false)
            ModelState.AddModelError(nameof(createdUserEntity.Login).ToLower(), "login is letters and digits");
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        var userInsert = userRepository.Insert(createdUserEntity);
        
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = userInsert.Id },
            userInsert.Id);
    }

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser(Guid userId, [FromBody] PatchUserDto userData)
    {
        if (userId == Guid.Empty || userData is null)
            return BadRequest();
        var putData = mapper.Map(userData, new UserEntity(userId));
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        userRepository.UpdateOrInsert(putData, out var isInserted);
        if (isInserted)
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userId },
                userId);
        return NoContent();
    }

    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser(Guid userId, [FromBody] JsonPatchDocument<PatchUserDto> patchDoc)
    {
        if (patchDoc is null)
            return BadRequest();

        var user = userRepository.FindById(userId);
        if (user is null || userId == Guid.Empty)
            return NotFound();
        
        var updateUserDto = mapper.Map(user, new PatchUserDto());
        patchDoc.ApplyTo(updateUserDto, ModelState);
        TryValidateModel(updateUserDto);
        if(!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        mapper.Map(updateUserDto, user);
        userRepository.Update(user);
        return NoContent();
    }
    
    [HttpDelete("{userId}")]
    public IActionResult DeleteUser(Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
            return NotFound();
        userRepository.Delete(userId);
        return NoContent();
    }

    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    public IActionResult GetUsers(int pageNumber = 1, int pageSize = 10)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Max(Math.Min(pageSize, 20), 1);
        
        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);
        
        var paginationHeader = new
        {
            previousPageLink = pageList.HasPrevious ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new {pageNumber = pageNumber - 1, pageSize}) : null,
            nextPageLink = pageList.HasNext ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new {pageNumber = pageNumber + 1, pageSize}) : null,
            totalCount = pageList.TotalCount,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages,
        };
        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        
        return Ok(users);
    }
    
    
    
    [HttpOptions(Name = nameof(GetUsersOptions))]
    public IActionResult GetUsersOptions()
    {
        Response.Headers.Add("Allow", "POST, GET, OPTIONS");
        return Ok();
    }
}