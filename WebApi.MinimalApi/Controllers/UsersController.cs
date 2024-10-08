using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

public class UserDto
{
    [Required]
    public string Login { get; set; }
    [DefaultValue("John")]
    public string FirstName { get; set; }
    [DefaultValue("Doe")]
    public string LastName { get; set; }
}

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

    [Produces("application/json", "application/xml")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [HttpHead("{userId}")]
    public ActionResult<Models.UserDto> GetUserById([FromRoute] System.Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
        {
            return NotFound();
        }
        if (HttpContext.Request.Method == HttpMethods.Head)
        {
            Response.Headers["Content-Type"] = "application/json; charset=utf-8";
            return Ok();
        }
        var userDto = mapper.Map<Models.UserDto>(user);
        return Ok(userDto);
    }
    
    [Produces("application/json", "application/xml")]
    [HttpPost]
    public IActionResult CreateUser([FromBody] UserDto user)
    {
        var createdUserEntity = mapper.Map<UserEntity>(user);
        if (user == null)
        {
            return BadRequest();
        }
        
        if (user.Login == null || !user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError(nameof(user.Login), "Сообщение об ошибке");
            return UnprocessableEntity(ModelState);
        }
        var insertedUser = userRepository.Insert(createdUserEntity);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = insertedUser.Id },
            insertedUser.Id);
    }
    
    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] System.Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
        {
            return NotFound();
        } 
        userRepository.Delete(userId);
        return NoContent();
    }
    
    [HttpGet]
    [Produces("application/json", "application/xml")]
    public ActionResult<IEnumerable<Models.UserDto>> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1)
        {
            pageNumber = 1;
        }
        if (pageSize < 1)
        {
            pageSize = 1;
        }
        if (pageSize > 20)
        {
            pageSize = 20;
        }
        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var users = mapper.Map<IEnumerable<Models.UserDto>>(pageList);
        var paginationHeader = new
        {
            previousPageLink = pageList.HasPrevious ?
                linkGenerator.GetUriByAction(HttpContext, nameof(GetUsers), values: new { pageNumber = pageNumber - 1, pageSize }) : null,
            nextPageLink = pageList.HasNext ?
                linkGenerator.GetUriByAction(HttpContext, nameof(GetUsers), values: new { pageNumber = pageNumber + 1, pageSize }) : null,
            totalCount = pageList.TotalCount,
            pageSize = pageSize,
            currentPage = pageNumber,
            totalPages = (int)Math.Ceiling((double)pageList.TotalCount / pageSize)
        };
        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        return Ok(users);
    }
    
    [HttpOptions]
    public IActionResult Options()
    {
        Response.Headers.Add("Allow", "GET, POST, OPTIONS");
        return Ok();
    }
}