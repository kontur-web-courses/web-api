using AutoMapper;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    [Produces("application/json", "application/xml")]
    [HttpGet("{userId:guid}", Name = nameof(GetUserById))]
    [HttpHead("{userId}")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user == null)
            return NotFound();

        if (!HttpMethods.IsHead(Request.Method)) return _mapper.Map<UserDto>(user);
        
        Response.ContentType = "application/json; charset=utf-8";
        return Ok();
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] CreatedUserDto? user)
    {
        if (user == null)
            return BadRequest();

        if (!string.IsNullOrEmpty(user.Login) && !user.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError("Login", "Login should contain only letters or digits");

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = _mapper.Map<UserEntity>(user);
        var createdUserEntity = _userRepository.Insert(userEntity);

        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }

    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdateUserDto user)
    {
        if (user == null || userId == Guid.Empty)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var userEntity = _mapper.Map(user, new UserEntity(userId));

        _userRepository.UpdateOrInsert(userEntity, out var isInserted);

        if (isInserted)
        {
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id);
        }

        return NoContent();
    }
    
    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc)
    {
        if (patchDoc == null)
            return BadRequest();
        
        var user = GetUserById(userId);
        
        if (user.Value is null) 
            return NotFound();
        
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var updateDto = new UpdateUserDto
        {
            Login = user.Value?.Login ?? "",
            FirstName = user.Value?.FullName ?? ""
        };
        
        patchDoc.ApplyTo(updateDto, ModelState);
        
        if (!ModelState.IsValid || !TryValidateModel(updateDto))
            return UnprocessableEntity(ModelState);

        UpdateUser(userId, updateDto);
        
        return NoContent();
    }
        
    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (_userRepository.FindById(userId) == null)
            return NotFound();
        
        _userRepository.Delete(userId);
        
        return NoContent();
    }

    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    public ActionResult<IEnumerable<UserDto>> GetUsers([FromQuery] int? pageNumber, [FromQuery] int? pageSize)
    {
        var number = Math.Max(pageNumber ?? 1, 1);
        var size = Math.Clamp(pageSize ?? 10, 1, 20);

        var page = _userRepository.GetPage(number, size);
        var users = _mapper.Map<IEnumerable<UserDto>>(page);

        var pagination = new
        {
            previousPageLink = page.HasPrevious
                ? Url.Link(nameof(GetUsers), new { pageNumber = number - 1, pageSize = size })
                : null,
            nextPageLink = page.HasNext
                ? Url.Link(nameof(GetUsers), new { pageNumber = number + 1, pageSize = size })
                : null,
            totalCount = page.TotalCount,
            pageSize = page.PageSize,
            currentPage = page.CurrentPage,
            totalPages = page.TotalPages
        };
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(pagination));

        return Ok(users);
    }

    [HttpOptions]
    public IActionResult GetUsersOptions()
    {
        Response.Headers.Append("Allow", "GET, POST, OPTIONS");
        return Ok();
    }
}