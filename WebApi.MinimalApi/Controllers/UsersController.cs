using System.Text.RegularExpressions;
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
    private readonly LinkGenerator _linkGenerator;
    private readonly IMapper _mapper;
    private readonly IUserRepository _userRepository;
    private readonly Regex regex = new(@"^[a-zA-Z0-9]+$");


    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
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
        if (user == null) return NotFound();

        if (HttpMethods.IsHead(Request.Method))
        {
            var acceptHeader = Request.Headers.Accept.ToString();
            Response.ContentType = acceptHeader.Contains("xml")
                ? "application/xml; charset=utf-8"
                : "application/json; charset=utf-8";
            return Ok();
        }

        var userDto = _mapper.Map(user, new UserDto());
        return Ok(userDto);
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] UserCreateDto user)
    {
        if (user == null) return BadRequest();

        if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

        if (!regex.IsMatch(user.Login))
        {
            ModelState.AddModelError("Login", "Login should be number or char");
            return UnprocessableEntity(ModelState);
        }

        var createdUserEntity = _userRepository.Insert(_mapper.Map(user, new UserEntity()));
        var createdUserDto = _mapper.Map<UserDto>(createdUserEntity);

        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserDto.Id);
    }

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UserPutDto? user)
    {
        if (user == null || userId == Guid.Empty) return BadRequest();

        if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

        var userEntity = new UserEntity(userId);
        _mapper.Map(user, userEntity);
        _userRepository.UpdateOrInsert(userEntity, out var isInserted);

        if (isInserted)
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id);

        return NoContent();
    }

    [HttpPatch("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserPutDto> patchDoc)
    {
        if (patchDoc == null) return BadRequest();

        var userEntity = _userRepository.FindById(userId);
        if (userEntity == null) return NotFound();

        var userDto = _mapper.Map<UserPutDto>(userEntity);
        patchDoc.ApplyTo(userDto, ModelState);
        TryValidateModel(userDto);

        if (!ModelState.IsValid) return UnprocessableEntity(ModelState); 

        _mapper.Map(userDto, userEntity);
        _userRepository.Update(userEntity);

        return NoContent();
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user == null) return NotFound();

        _userRepository.Delete(userId);

        return NoContent();
    }

    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    public ActionResult<IEnumerable<UserDto>> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 20);

        var pageList = _userRepository.GetPage(pageNumber, pageSize);
        var previousPageLink = pageList.HasPrevious
            ? _linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber - 1, pageSize })
            : null; 

        var nextPageLink = _linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber + 1, pageSize });

        var paginationHeader = new
        {
            previousPageLink,
            nextPageLink,
            totalCount = pageList.TotalCount,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages
        };

        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        var users = _mapper.Map<IEnumerable<UserDto>>(pageList);
        return Ok(users);
    }
    
    [HttpOptions]
    public IActionResult UsersOptions()
    {
        Response.Headers.Add("Allow", "POST, GET, OPTIONS");
        return Ok();
    }
}