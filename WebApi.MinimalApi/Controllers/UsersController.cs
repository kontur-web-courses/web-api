using System.Diagnostics;
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
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;
    private readonly LinkGenerator linkGenerator;
    private static readonly string[] methods = new string[] { "GET", "POST", "OPTIONS" };

    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    public UsersController(
        IUserRepository userRepository,
        IMapper mapper,
        LinkGenerator linkGenerator
    )
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
        this.linkGenerator = linkGenerator;
    }

    [HttpHead("{userId}")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var userEntity = userRepository.FindById(userId);
        if (userEntity is null)
            return NotFound();

        if (HttpMethods.IsHead(Request.Method))
        {
            Response.ContentType = "application/json; charset=utf-8";
            return Ok();
        }

        return Ok(mapper.Map<UserDto>(userEntity));
    }

    private IActionResult CreateUserInternal(CreateUserDto createUserDto)
    {
        var userEntity = mapper.Map<UserEntity>(createUserDto);
        userEntity = userRepository.Insert(userEntity);
        return CreatedAtRoute(nameof(GetUserById), new { userId = userEntity.Id }, userEntity.Id);
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] CreateUserDto? createUserDto)
    {
        if (createUserDto is null)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        return CreateUserInternal(createUserDto);
    }

    [HttpPut("{rawUserId}")]
    public IActionResult PutUser(
        [FromRoute] string rawUserId,
        [FromBody] UpdateUserDto? updateUserDto
    )
    {
        if (!Guid.TryParse(rawUserId, out var userId))
            return BadRequest();

        if (updateUserDto is null)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = userRepository.FindById(userId);
        if (userEntity is null)
            return CreateUserInternal(mapper.Map<CreateUserDto>(updateUserDto));

        mapper.Map(updateUserDto, userEntity);
        userRepository.Update(userEntity);
        return NoContent();
    }

    [HttpPatch("{rawUserId}")]
    public IActionResult PatchUser(
        [FromRoute] Guid? rawUserId,
        [FromBody] JsonPatchDocument<UpdateUserDto>? patchDoc
    )
    {
        if (rawUserId is null)
            return NotFound();

        var userId = rawUserId.Value;

        if (patchDoc is null)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = userRepository.FindById(userId);
        if (userEntity is null)
            return NotFound();

        var updateUserDto = mapper.Map<UpdateUserDto>(userEntity);
        patchDoc.ApplyTo(updateUserDto, ModelState);
        TryValidateModel(updateUserDto);

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        mapper.Map(updateUserDto, userEntity);
        userRepository.Update(userEntity);
        return NoContent();
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var userEntity = userRepository.FindById(userId);
        if (userEntity is null)
            return NotFound();

        userRepository.Delete(userId);
        return NoContent();
    }

    [HttpGet(Name = nameof(GetUsers))]
    public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 20);

        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);

        var paginationHeader = new
        {
            previousPageLink = pageNumber > 1 ? MakePageLink(pageNumber - 1, pageSize) : null,
            nextPageLink = MakePageLink(pageNumber + 1, pageSize),
            pageSize,
            currentPage = pageNumber,
            totalCount = pageList.Count,
            totalPages = 1,
        };
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        return Ok(users);
    }

    private string MakePageLink(int pageNumber, int pageSize) =>
        linkGenerator.GetUriByRouteValues(
            HttpContext,
            nameof(GetUsers),
            new { pageNumber, pageSize }
        )!;

    [HttpOptions]
    public IActionResult OptionsUsers()
    {
        Response.Headers.Allow = string.Join(", ", methods);
        return Ok();
    }
}
