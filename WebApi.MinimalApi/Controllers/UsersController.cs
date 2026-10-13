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
        if (user is null)
        {
            return NotFound();
        }

        var userDto = _mapper.Map<UserDto>(user);
        if (HttpMethods.IsHead(Request.Method))
        {
            Response.ContentType = "application/json; charset=utf-8";
            return Ok();
        }

        return Ok(userDto);
    }

    [HttpPost(Name = nameof(CreateUser))]
    [Produces("application/json", "application/xml")]
    public ActionResult<Guid> CreateUser([FromBody] UserCreateRequest? user)
    {
        if (user is null)
            return BadRequest();

        if (string.IsNullOrEmpty(user.Login) || !user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError(nameof(user.Login), "Login is invalid");
        }

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = _mapper.Map<UserEntity>(user);
        var entity = _userRepository.Insert(userEntity);

        return CreatedAtRoute(
            nameof(CreateUser),
            new { userId = entity.Id },
            entity.Id);
    }

    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromBody] UserUpdateRequest? user, Guid userId)
    {
        if (user is null || userId == Guid.Empty)
            return BadRequest();
        if (string.IsNullOrEmpty(user.Login))
        {
            ModelState.AddModelError(nameof(user.Login), "Login is invalid");
        }

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = _mapper.Map<UserEntity>(user, opt => { opt.Items["Id"] = userId; });

        _userRepository.UpdateOrInsert(userEntity, out var isInsterted);
        if (!isInsterted)
            return NoContent();

        var userDto = _mapper.Map<UserDto>(userEntity);

        return CreatedAtRoute(
            nameof(CreateUser),
            userDto);
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser(Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user is null)
            return NotFound();

        _userRepository.Delete(userId);
        return NoContent();
    }

    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser([FromBody] JsonPatchDocument<UpdateDto>? patchDoc, Guid userId)
    {
        if (patchDoc is null)
            return BadRequest();
        var userEntity = _userRepository.FindById(userId);
        if (userEntity is null)
            return NotFound();
        if (!ModelState.IsValid)
            return NotFound(ModelState);
        var updateDto = _mapper.Map<UpdateDto>(userEntity);

        patchDoc.ApplyTo(updateDto, ModelState);

        if (!TryValidateModel(updateDto) || string.IsNullOrEmpty(updateDto.FirstName) || string.IsNullOrEmpty(updateDto.LastName))
            return UnprocessableEntity(ModelState);


        var updatedUserEntity = _mapper.Map<UserEntity>(updateDto);
        _userRepository.Update(updatedUserEntity);

        return NoContent();
    }

    [HttpGet(Name = nameof(GetUsers))]
    public ActionResult<IEnumerable<UserDto>> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var pageList = _userRepository.GetPage(pageNumber, pageSize);
        var paginationHeader = new
        {
            previousPageLink = pageNumber - 1 > 0
                ? _linkGenerator.GetUriByRouteValues(HttpContext, "GetUsers", new
                {
                    pageNumber = pageNumber - 1,
                    pageSize = pageSize
                })
                : null,
            nextPageLink = pageNumber + 1 <= pageList.TotalPages
                ? _linkGenerator.GetUriByRouteValues(HttpContext, "GetUsers", new
                {
                    pageNumber = (pageNumber + 1),
                    pageSize = pageSize
                })
                : null,
            totalCount = pageList.TotalCount,
            pageSize = Math.Clamp(pageSize, 1, 20),
            currentPage = Math.Max(1, pageNumber),
            totalPages = pageList.TotalPages,
        };
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

        var users = _mapper.Map<IEnumerable<UserDto>>(pageList);
        return Ok(users);
    }


    [HttpOptions]
    public IActionResult Options()
    {
        Response.Headers.Append("Allow", "GET, POST, OPTIONS");
        return Ok();
    }
}