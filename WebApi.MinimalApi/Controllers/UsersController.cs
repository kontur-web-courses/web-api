using System.Text.RegularExpressions;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
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

    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
        this.linkGenerator = linkGenerator;
    }

    [Produces("application/json", "application/xml")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);

        if (user == null)
            return NotFound();

        // var userDto = new UserDto
        // {
        //     Id = user.Id,
        //     CurrentGameId = user.CurrentGameId,
        //     FullName = $"{user.LastName} {user.FirstName}",
        //     Login = user.Login,
        //     GamesPlayed = user.GamesPlayed
        // };

        var userDto = mapper.Map<UserDto>(user);

        return Ok(userDto);
    }

    [Produces("application/json", "application/xml")]
    [HttpPost]
    public IActionResult CreateUser([FromBody] UserDtoForCreating? user)
    {
        if (user == null)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        if (!string.IsNullOrEmpty(user.Login) && !user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("Login", "Логин должен состоять только из букв и цифр");
            return UnprocessableEntity(ModelState);
        }

        var userEntity = mapper.Map<UserEntity>(user);
        var createdUserEntity = userRepository.Insert(userEntity);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }

    [Produces("application/json", "application/xml")]
    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromRoute] string userId, [FromBody] UpdateUserDto user)
    {
        if (user == null)
            return BadRequest();
        
        if (!Guid.TryParse(userId, out var parsedUserId))
            return BadRequest();
        
        if (string.IsNullOrEmpty(user.Login))
            return UnprocessableEntity(JObject.FromObject(new { login = new JArray("Login is required") }));

        if (!Regex.IsMatch(user.Login, @"^[a-zA-Z0-9]+$"))
            return UnprocessableEntity(JObject.FromObject(new { login = new JArray("Login contains invalid characters") }));

        if (string.IsNullOrEmpty(user.FirstName))
            return UnprocessableEntity(JObject.FromObject(new { firstName = new JArray("First name is required") }));

        if (string.IsNullOrEmpty(user.LastName))
            return UnprocessableEntity(JObject.FromObject(new { lastName = new JArray("Last name is required") }));

        var existingUser = userRepository.FindById(parsedUserId);

        if (existingUser == null)
        {
            var userEntity = new UserEntity(parsedUserId)
            {
                Login = user.Login,
                FirstName = user.FirstName,
                LastName = user.LastName,
                GamesPlayed = 0,
                CurrentGameId = null
            };

            userRepository.UpdateOrInsert(userEntity, out var isInserted);
            
            var createdId = userEntity.Id.ToString();
            
            return CreatedAtAction(nameof(GetUserById), new { userId = createdId }, createdId);
        }
        else
        {
            existingUser.Login = user.Login;
            existingUser.FirstName = user.FirstName;
            existingUser.LastName = user.LastName;

            userRepository.UpdateOrInsert(existingUser, out var isInserted);
            
            return NoContent();
        }
    }
    
    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser([FromRoute] string userId, [FromBody] JsonPatchDocument<UserDtoForUpdating>? patchDoc)
    {
        if (patchDoc == null)
            return BadRequest();

        if (!Guid.TryParse(userId, out var userGuid))
            return NotFound();

        var existingUser = userRepository.FindById(userGuid);
        if (existingUser == null)
            return NotFound();

        var userToPatch = mapper.Map<UserDtoForUpdating>(existingUser);

        patchDoc.ApplyTo(userToPatch, ModelState);

        TryValidateModel(userToPatch);

        if (!ModelState.IsValid)
        {
            return new ObjectResult(ModelState)
            {
                StatusCode = StatusCodes.Status422UnprocessableEntity,
                ContentTypes = { "application/json" }
            };
        }

        mapper.Map(userToPatch, existingUser);

        userRepository.UpdateOrInsert(existingUser, out var isInserted);

        return NoContent();
    }
    
    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);

        if (user == null)
            return NotFound();
        
        userRepository.Delete(userId);
        return NoContent();
        
    }

    [HttpHead("{userId}")]
    public IActionResult HeadUser([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        
        if (user == null)
            return NotFound();
        
        Response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        
        return Ok();
    }

    [HttpGet(Name = "GetUsers")]
    [Produces("application/json", "application/xml")]
    public IActionResult GetUsers([FromQuery, Range(1, Int32.MaxValue)] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        pageNumber = Math.Clamp(pageNumber, 1, Int32.MaxValue);
        pageSize = Math.Clamp(pageSize, 1, 20);

        var totalUsers = GetTotalUserCount();
        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);
        var totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);
        
        var nextPageLink = linkGenerator.GetUriByRouteValues(HttpContext, "GetUsers", new {pageNumber = pageNumber + 1, pageSize = pageSize});
        var prevPageLink = linkGenerator.GetUriByRouteValues(HttpContext, "GetUsers", new {pageNumber = pageNumber - 1, pageSize = pageSize});
        
        var paginationHeader = new
        {
            previousPageLink = pageNumber > 1 ? prevPageLink : null,
            nextPageLink = nextPageLink,
            totalCount = totalUsers,
            pageSize = pageSize,
            currentPage = pageNumber,
            totalPages = totalPages,
        };
        
        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        
        return Ok(users);
    }

    [HttpOptions]
    public IActionResult Options()
    {
        Response.Headers.Add("Allow", "POST,GET,OPTIONS");
        return Ok();
    }

    private int GetTotalUserCount()
    {
        var totalUsers = 0;
        var tempPageNum = 1;
        
        while (userRepository.GetPage(tempPageNum, 20).Count()!= 20 && !userRepository.GetPage(tempPageNum, 20).Any())
        {
            totalUsers+=20;
            tempPageNum++;
        }
        
        totalUsers += userRepository.GetPage(tempPageNum, 20).Count();
        
        return totalUsers;
    }
}