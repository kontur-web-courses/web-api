using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[Produces("application/json", "application/xml")]
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
    
    [Produces("application/json", "application/xml")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [HttpHead("{userId}")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user == null)
            return NotFound();
        var userDto = _mapper.Map<UserDto>(user);
        if (HttpMethods.IsHead(Request.Method))
        {
            Response.ContentType = $"{Request.Headers.Accept}; charset=utf-8";
            return Ok();
        }

        return Ok(userDto);
     
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] UserPost? user)
    {
        if (user is null)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        if (!user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError(nameof(user.Login), "Login should contain only letters or digits");
            return UnprocessableEntity(ModelState);
        }
        
        var userEntity = _mapper.Map<UserEntity>(user);
        var userEntityWithId = _userRepository.Insert(userEntity);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = userEntityWithId.Id },
            userEntityWithId.Id);
    }

    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UserPut? updateDto)
    {
        if (updateDto is null || userId == Guid.Empty)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        var userEntity = new UserEntity(userId);
        
        userEntity.Login = updateDto.Login;
        userEntity.FirstName = updateDto.FirstName;
        userEntity.LastName = updateDto.LastName;

        _userRepository.UpdateOrInsert(userEntity, out var success);

        return success ? Created("user", userEntity) : NoContent();
    }
    
    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserPut>? patchDoc)
    {
        if (patchDoc is null)
            return BadRequest();
        
        var user = _userRepository.FindById(userId);
        if (user is null)
            return NotFound();
        
        var updateDto = _mapper.Map<UserPut>(user);
        patchDoc.ApplyTo(updateDto, ModelState);
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        if (!TryValidateModel(updateDto))
            return UnprocessableEntity(ModelState);
        
        user.Login = updateDto.Login;
        user.FirstName = updateDto.FirstName;
        user.LastName = updateDto.LastName;
        _userRepository.Update(user);

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

    [HttpGet(Name = nameof(GetUsers))]
    public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 20);

        var pageList = _userRepository.GetPage(pageNumber, pageSize);
        var users = _mapper.Map<IEnumerable<UserDto>>(pageList);

        var totalCount = pageList.TotalCount;
        var totalPages = (int) Math.Ceiling((double)totalCount / pageSize);

        string? previousPageLink = null;
        string? nextPageLink = null;

        if (pageNumber > 1)
        {
            previousPageLink = _linkGenerator.GetUriByRouteValues(
                HttpContext,
                nameof(GetUsers),
                new { pageNumber = pageNumber - 1, pageSize });
        }

        if (pageNumber < totalPages)
        {
            nextPageLink = _linkGenerator.GetUriByRouteValues(
                HttpContext,
                nameof(GetUsers),
                new { pageNumber = pageNumber + 1, pageSize });
        }

        var paginationHeader = new
        {
            previousPageLink,
            nextPageLink,
            totalCount,
            pageSize,
            currentPage = pageNumber,
            totalPages
        };

        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

        return Ok(users);
    }

    [HttpOptions]
    public IActionResult OptionsUsers()
    {
        Response.Headers.Add("Allow", "GET, POST, OPTIONS");
        return Ok();
    }
}