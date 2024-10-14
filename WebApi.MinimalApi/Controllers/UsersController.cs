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
    private IUserRepository userRepository;
    private IMapper mapper;
    private LinkGenerator linkGenerator;
    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
        this.linkGenerator = linkGenerator;
    }

    [HttpHead("{userId}")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();
        var userDto = mapper.Map<UserDto>(user);
        if (Request.Method == "HEAD") 
        {
            Response.Headers.Append("Content-Type", "application/json; charset=utf-8");
            return Ok();
        }

        return Ok(userDto);
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] UserCreationDto user)
    {
        if (user == null)
            return BadRequest();
        if (ModelState.IsValid && !user.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError("Login", "Unaccepted symbols, only letter or digits are acceptable");
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var userEntity = mapper.Map<UserEntity>(user);
        var insetedUser = userRepository.Insert(userEntity);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = insetedUser.Id },
            insetedUser.Id);
    }

    [HttpPut("{userToUpdateId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromBody] UserUpdateDto user, [FromRoute] Guid userToUpdateId)
    {
        if (user == null || userToUpdateId == Guid.Empty)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var userEntity = new UserEntity(userToUpdateId);
        userEntity = mapper.Map(user, userEntity);
        var isInserted = false;
        userRepository.UpdateOrInsert(userEntity,out isInserted);
        if (isInserted)
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userToUpdateId},
                userToUpdateId);
        return NoContent();
    }


    [HttpPatch("{userToUpdateId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser([FromBody] JsonPatchDocument<UserUpdateDto> patchDoc, [FromRoute] Guid userToUpdateId)
    {
        if (patchDoc == null)
            return BadRequest();
        var acturalUser = userRepository.FindById(userToUpdateId);
        if (acturalUser == null)
            return NotFound();

        var updateDto = new UserUpdateDto();
        patchDoc.ApplyTo(updateDto, ModelState);
        TryValidateModel(updateDto);
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = new UserEntity(userToUpdateId);
        userEntity = mapper.Map(updateDto, userEntity);
        userRepository.Update(userEntity);
        return NoContent();
    }

    [HttpDelete("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var acturalUser = userRepository.FindById(userId);
        if (acturalUser == null)
            return NotFound();
        userRepository.Delete(userId);
        return NoContent();
    }

    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10) 
    {
        var page = userRepository.GetPage(pageNumber, pageSize);
        pageSize = Math.Min(pageSize, 20);
        pageSize = Math.Max(pageSize, 1);
        pageNumber = Math.Max(pageNumber, 1);
        var users = mapper.Map<IEnumerable<UserDto>>(page);
        var paginationHeader = new  
        {
            previousPageLink = page.HasPrevious 
                ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new {pageNumber = pageNumber - 1, pageSize})
                : null,
            nextPageLink = page.HasNext
            ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber + 1, pageSize })
            : null,
            page.TotalCount,
            pageSize,
            currentPage = pageNumber,
            page.TotalPages,
        };
        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        return Ok(users);
    }

    [HttpOptions]
    [Produces("application/json", "application/xml")]
    public IActionResult GetOptions()
    {
        Response.Headers.Add("Allow", new[] { "POST", "GET", "OPTIONS" });
        return Ok();
    }
}