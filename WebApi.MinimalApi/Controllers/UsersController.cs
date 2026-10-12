using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly LinkGenerator _linkGenerator;

    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 10;
    private const int MinPageNumber = 1;
    private const int MinPageSize = 1;
    private const int MaxPageSize = 20;

    public UsersController(IUserRepository userRepository, AutoMapper.IMapper mapper, LinkGenerator linkGenerator)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _linkGenerator = linkGenerator;
    }

    [Produces("application/json", "application/xml")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        return user == null ? NotFound() : Ok(_mapper.Map<UserDto>(user));
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] CreateUserDto user)
    {
        if (user == null)
            return BadRequest();

        if (user.Login != null && !user.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError(nameof(user.Login), "Login should contain only letters or digits");

        if (ModelState.IsValid)
        {
            var createdUserEntity = _userRepository.Insert(_mapper.Map<UserEntity>(user));
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
        }

        return UnprocessableEntity(ModelState);
    }

    
    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdateUserDto? userDto)
    {
        if (userDto == null || userId == Guid.Empty)
            return BadRequest();
        
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }
        
        var user = _mapper.Map(userDto, new UserEntity(userId));
        _userRepository.UpdateOrInsert(user, out var isInserted);
        
        if (isInserted)
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = user.Id },
                user.Id);
        return NoContent();
    }

    [HttpPatch("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserDto>? patchDoc)
    {
        if (patchDoc is null)
            return BadRequest();
        
        var user = _userRepository.FindById(userId);
        if (user is null)
            return NotFound();
        
        var updateUserDto = _mapper.Map<UpdateUserDto>(user);
        patchDoc.ApplyTo(updateUserDto, ModelState);
        
        TryValidateModel(updateUserDto);
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        _mapper.Map(updateUserDto, user);
        _userRepository.Update(user);
        
        return NoContent();
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user is null)
            return NotFound();
        
        _userRepository.Delete(userId);
        return NoContent();
    }

    [HttpHead("{userId}")]
    public IActionResult HeadUserById([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user is null)
            return NotFound();

        Response.ContentType = "application/json; charset=utf-8";
        return Ok();
    }
    
    [HttpGet(Name = nameof(GetUsers))]
    public IActionResult GetUsers(
        [FromQuery] int pageNumber = DefaultPageNumber,
        [FromQuery] int pageSize = DefaultPageSize)
    {
        pageNumber = Math.Max(pageNumber, MinPageNumber);
        pageSize = Math.Clamp(pageSize, MinPageSize, MaxPageSize);

        var pageList = _userRepository.GetPage(pageNumber, pageSize);

        var previousPageLink = pageList.HasPrevious
            ? _linkGenerator.GetUriByRouteValues(
                HttpContext,
                nameof(GetUsers),
                new { pageNumber = pageNumber - 1, pageSize })
            : null;
        var nextPageLink = pageList.HasNext
            ? _linkGenerator.GetUriByRouteValues(
                HttpContext,
                nameof(GetUsers),
                new { pageNumber = pageNumber + 1, pageSize })
            : null;

        var paginationHeader = new
        {
            previousPageLink,
            nextPageLink,
            totalCount = pageList.TotalCount,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages
        };
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

        var users = _mapper.Map<IEnumerable<UserDto>>(pageList);
        return Ok(users);
    }

    [HttpOptions]
    public IActionResult GetUserOptions()
    {
        Response.Headers.AppendCommaSeparatedValues(
            HeaderNames.Allow,
            HttpMethods.Get, HttpMethods.Post, HttpMethods.Options
        );

        return Ok();
    }
}