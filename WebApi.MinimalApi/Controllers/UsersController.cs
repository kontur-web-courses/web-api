using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json", "application/xml")]
public class UsersController : ControllerBase
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

    [HttpGet("{userId:guid}")]
    [HttpHead("{userId:guid}")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var userEntity = _userRepository.FindById(userId);
        if (userEntity == null)
        {
            return NotFound();
        }

        if (Request.Method == HttpMethods.Head)
        {
            Response.Headers.ContentType = "application/json; charset=utf-8";
            return Ok();
        }

        var userDto = _mapper.Map<UserDto>(userEntity);
        return Ok(userDto);
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] UserCreateDto? userCreateDto)
    {
        if (userCreateDto == null)
            return BadRequest();

        if (string.IsNullOrEmpty(userCreateDto.Login) || !userCreateDto.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("Login", "Invalid login format");
            return UnprocessableEntity(ModelState);
        }

        var userEntity = _mapper.Map<UserEntity>(userCreateDto);
        var createdUser = _userRepository.Insert(userEntity);

        return CreatedAtAction(
            actionName: nameof(GetUserById),
            routeValues: new { userId = createdUser.Id },
            value: createdUser.Id);
    }

    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UserUpdateDto? userUpdateDto)
    {
        if (userUpdateDto == null || userId == Guid.Empty)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var updatedUser = _mapper.Map(userUpdateDto, new UserEntity(userId));

        _userRepository.UpdateOrInsert(updatedUser, out var isNewUser);
    
        if (isNewUser)
        {
            return CreatedAtAction(
                actionName: nameof(GetUserById),
                routeValues: new { userId = updatedUser.Id },
                value: updatedUser.Id);
        }

        return NoContent();
    }

    [HttpPatch("{userId:guid}")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserUpdateDto>? patchDocument)
    {
        if (patchDocument == null)
        {
            return BadRequest();
        }

        var existingUser = _userRepository.FindById(userId);
        if (existingUser == null || userId == Guid.Empty)
        {
            return NotFound();
        }

        var userToUpdate = _mapper.Map<UserUpdateDto>(existingUser);
        patchDocument.ApplyTo(userToUpdate, ModelState);

        if (!TryValidateModel(userToUpdate))
        {
            return UnprocessableEntity(ModelState);
        }

        var updatedEntity = _mapper.Map(userToUpdate, existingUser);
        _userRepository.Update(updatedEntity);

        return NoContent();
    }

    [HttpDelete("{userId:guid}")]
    public IActionResult RemoveUser([FromRoute] Guid userId)
    {
        var userEntity = _userRepository.FindById(userId);
        if (userEntity == null)
        {
            return NotFound();
        }

        _userRepository.Delete(userId);
        return NoContent();
    }

    [HttpGet]
    public ActionResult<IEnumerable<UserDto>> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 20);

        var userPage = _userRepository.GetPage(pageNumber, pageSize);
        var userDtos = _mapper.Map<IEnumerable<UserDto>>(userPage);

        var paginationMetadata = new
        {
            previousPageLink = userPage.HasPrevious ? 
                _linkGenerator.GetUriByAction(HttpContext, nameof(GetUsers), values: new 
                { 
                    pageNumber = pageNumber - 1, 
                    pageSize 
                }) : null,
            nextPageLink = userPage.HasNext ? 
                _linkGenerator.GetUriByAction(HttpContext, nameof(GetUsers), values: new 
                { 
                    pageNumber = pageNumber + 1, 
                    pageSize 
                }) : null,
            totalCount = userPage.TotalCount,
            pageSize,
            currentPage = pageNumber,
            totalPages = (int)Math.Ceiling(userPage.TotalCount / (double)pageSize)
        };

        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationMetadata));

        return Ok(userDtos);
    }

    [HttpOptions]
    public IActionResult GetOptions()
    {
        Response.Headers.Append("Allow", "GET, POST, OPTIONS");
        return Ok();
    }
}