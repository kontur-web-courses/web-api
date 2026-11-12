using System.Text.RegularExpressions;
using System.Xml.Serialization;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public partial class UsersController : Controller
{
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;
    private readonly LinkGenerator linkGenerator;
    private static readonly Regex AllowedLoginRegex = MyRegex();
    
    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
        this.linkGenerator = linkGenerator;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public IActionResult GetUserById([FromRoute] Guid userId, [FromHeader(Name = "Accept")] string acceptHeader)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
        {
            return NotFound();
        }

        var userDto = mapper.Map<UserDto>(user);

        if (acceptHeader.Contains("application/json"))
        {
            return new JsonResult(userDto)
            {
                StatusCode = 200,
                ContentType = "application/json; charset=utf-8"
            };
        }

        if (!acceptHeader.Contains("application/xml"))
            return StatusCode(406);
        
        using var stringWriter = new StringWriter();
        new XmlSerializer(typeof(UserDto)).Serialize(stringWriter, userDto);
        var xml = stringWriter.ToString();
        return Content(xml, "application/xml; charset=utf-8");

    }

    [HttpPost] 
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] UserCreateDto? userCreateDto)
    {
        var acceptHeader = Request.Headers.Accept.FirstOrDefault() ?? string.Empty;
        if (acceptHeader.Contains("text/plain"))
            return StatusCode(StatusCodes.Status406NotAcceptable);

        if (userCreateDto == null)
            return StatusCode(StatusCodes.Status400BadRequest);

        if (string.IsNullOrWhiteSpace(userCreateDto.Login) || !userCreateDto.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError(nameof(userCreateDto.Login), "Логин не прошел валидацию");
            return UnprocessableEntity(new { login  = "Login is required"});
        }

        var newUser = new UserEntity
        {
            Login = userCreateDto.Login,
            FirstName = userCreateDto.FirstName,
            LastName = userCreateDto.LastName,
            GamesPlayed = 0,
            CurrentGameId = null
        };
        var inserted = userRepository.Insert(newUser);

        return CreatedAtRoute(
            nameof(GetUserById), 
            new { userId = inserted.Id },
            inserted.Id);
    }

    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdateUserRequest? updateUserDto)
    {
        if (updateUserDto == null || userId == Guid.Empty)
            return BadRequest();

        if (string.IsNullOrWhiteSpace(updateUserDto.login))
            return UnprocessableEntity(new { login = "Login is required" });

        if (!AllowedLoginRegex.IsMatch(updateUserDto.login))
            return UnprocessableEntity(new { login = "Invalid login" });

        if (string.IsNullOrWhiteSpace(updateUserDto.firstName))
            return UnprocessableEntity(new { firstName = "First name is required" });

        if (string.IsNullOrWhiteSpace(updateUserDto.lastName))
            return UnprocessableEntity(new { lastName = "Last name is required" });

        var user = new UserEntity(userId)
        {
            Login = updateUserDto.login,
            FirstName = updateUserDto.firstName,
            LastName = updateUserDto.lastName
        };
        userRepository.UpdateOrInsert(user, out var isInserted);

        if (!isInserted)
            return NoContent();
        Response.Headers.Location = Request.Path.Value;

        return new ContentResult
        {
            Content = JsonConvert.SerializeObject(userId),
            ContentType = "application/json; charset=utf-8",
            StatusCode = StatusCodes.Status201Created
        };
    }

    [HttpPatch("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserRequest>? patchDoc)
    {
        if (patchDoc == null)
            return BadRequest();

        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();

        var updateRequest = mapper.Map<UpdateUserRequest>(user);
        
        patchDoc.ApplyTo(updateRequest, ModelState);
        
        if (!TryValidateModel(updateRequest))
            return UnprocessableEntity(ModelState);

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        mapper.Map(updateRequest, user);
        userRepository.Update(user);

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
        
        if (user is null)
            return NotFound();
        
        Response.Headers.Append("Content-Type", "application/json; charset=utf-8");
        return Ok();
    }

    [HttpGet(Name = nameof(GetUsersWithPagination))]
    [Produces("application/json", "application/xml")]
    public IActionResult GetUsersWithPagination([FromQuery] int pageSize = 10, [FromQuery] int pageNumber = 1)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= PageList<UserEntity>.MinPageSize) pageSize = PageList<UserEntity>.MinPageSize;
        if (pageSize > PageList<UserEntity>.MaxPageSize) pageSize = PageList<UserEntity>.MaxPageSize;
        
        var resultPage = userRepository.GetPage(pageNumber, pageSize);
        var users = mapper.Map<IEnumerable<UserDto>>(resultPage);
        var pagination = new Pagination
        {
            PreviousPageLink = resultPage.HasPrevious
                ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsersWithPagination), new { pageNumber = resultPage.CurrentPage - 1, pageSize }) 
                : null,
            NextPageLink = resultPage.HasNext
                ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsersWithPagination), new { pageNumber = resultPage.CurrentPage + 1, pageSize })
                : null,
            TotalCount = (int)resultPage.TotalCount,
            CurrentPage = resultPage.CurrentPage,
            PageSize = resultPage.PageSize,
            TotalPages = resultPage.TotalPages
        };
        
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(pagination));
        
        return Ok(users);
    }

    [HttpOptions]
    public IActionResult GetUserOptions()
    {
        Response.Headers.Append("Allow", "POST, GET, OPTIONS");
        return Ok();
    }

    [GeneratedRegex(@"^[A-Za-z0-9_\-]+$")]
    private static partial Regex MyRegex();
}