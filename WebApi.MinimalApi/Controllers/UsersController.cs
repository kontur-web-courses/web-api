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
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly LinkGenerator _linkGenerator;
    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _linkGenerator = linkGenerator;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user == null)
            return NotFound();
        if (HttpMethods.IsHead(Request.Method))
        {
            Response.ContentType = "application/json; charset=utf-8";
            return Ok();
        }

        var userDto = _mapper.Map<UserDto>(user);
        return Ok(userDto);
    }
    
    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] CreateUserDto? user)
    {
        if (user is null)
            return BadRequest();
        if (user.Login != null && !user.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError("Login", "Логин должен состоять только из цифр и букв");
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
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdateUserDto? user)
    {
        if (userId == Guid.Empty)
            return BadRequest();
        if (user is null)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        var userEntity = _mapper.Map(user, new UserEntity(userId));
        _userRepository.UpdateOrInsert(userEntity, out var isInserted);
        if (isInserted)
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId },
                userId);
        
        return NoContent();
    }
    
    [HttpPatch("{userId}")]
    [Consumes("application/json-patch+json")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser ([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserDto>? patchDoc)
    {
        if (patchDoc == null)
            return BadRequest();
        var user = _userRepository.FindById(userId);
        if (user == null)
            return NotFound();
        var updateDto = new UpdateUserDto();
        patchDoc.ApplyTo(updateDto, ModelState);

        if (!TryValidateModel(updateDto))
            return UnprocessableEntity(ModelState);
        var userEntity = _mapper.Map(updateDto, new UserEntity(userId));
        _userRepository.Update(userEntity);
        return NoContent();
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user == null)
            return NotFound();
        _userRepository.Delete(userId);
        return NoContent();
    }

    [HttpGet(Name = nameof(GetAllUsers))]
    [Produces("application/json", "application/xml")]
    public IActionResult GetAllUsers([FromQuery] GetAllUsersDto? usersDto)
    {
        if (usersDto is null)
            return BadRequest();
        
        var pageNumber = usersDto.PageNumber;
        var pageSize = usersDto.PageSize;
        
        var pageList = _userRepository.GetPage(pageNumber, pageSize);
        var users = _mapper.Map<IEnumerable<UserDto>>(pageList);
        
        var previousPageLink = pageList.HasPrevious
            ? _linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetAllUsers),
                new { pageNumber = pageNumber - 1, pageSize })
            : null;
        var nextPageLink = pageList.HasNext
            ? _linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetAllUsers),
                new { pageNumber = pageNumber + 1, pageSize })
            : null;
        var paginationHeader = new
        {
            previousPageLink,
            nextPageLink,
            totalCount = pageList.TotalCount,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages,
        };
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        return Ok(users);
    }
    
    [HttpOptions]
    public IActionResult Options()
    {
        Response.Headers.Append("Allow", "OPTIONS, POST, GET");
        return Ok();
    }
}