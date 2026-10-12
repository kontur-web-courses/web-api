using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly LinkGenerator _linkGenerator;
    private readonly IMapper _mapper;
    private readonly IUserRepository _userRepository;

    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _linkGenerator = linkGenerator;
    }

    [HttpGet("{userId:guid}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);

        if (user is null)
            return NotFound();

        return Ok(_mapper.Map<UserDto>(user));
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public ActionResult<Guid> CreateUser([FromBody] CreateUserDto? user)
    {
        if (user is null)
            return BadRequest();

        if (!string.IsNullOrEmpty(user.Login) && !user.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError("Login", "Login should contain only letters or digits");

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = _mapper.Map<UserEntity>(user);
        var createdUserEntity = _userRepository.Insert(userEntity);

        return CreatedAtRoute(nameof(GetUserById), new { createdUserEntity.Id }, createdUserEntity.Id);
    }

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdateUserDto? user)
    {
        if (userId == Guid.Empty || user is null)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = _userRepository.FindById(userId) ?? new UserEntity(userId);
        _mapper.Map(user, userEntity);
        _userRepository.UpdateOrInsert(userEntity, out var isInserted);

        return isInserted
            ? CreatedAtRoute(nameof(GetUserById), new { userEntity.Id }, userEntity.Id)
            : NoContent();
    }

    [HttpPatch("{userId:guid}")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, JsonPatchDocument<UpdateUserDto>? patchDocument)
    {
        if (patchDocument is null)
            return BadRequest();

        var userEntity = _userRepository.FindById(userId);

        if (userEntity is null)
            return NotFound();

        var updateUserDto = _mapper.Map<UpdateUserDto>(userEntity);
        patchDocument.ApplyTo(updateUserDto, ModelState);
        TryValidateModel(updateUserDto);

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        _mapper.Map(updateUserDto, userEntity);
        _userRepository.Update(userEntity);

        return NoContent();
    }

    [HttpDelete("{userId:guid}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var userEntity = _userRepository.FindById(userId);
        if (userEntity is null)
            return NotFound();

        _userRepository.Delete(userId);
        return NoContent();
    }

    [HttpHead("{userId:guid}")]
    public IActionResult HeadUserById([FromRoute] Guid userId)
    {
        if (_userRepository.FindById(userId) is null)
            return NotFound();

        Response.ContentType = "application/json; charset=utf-8";
        return Ok();
    }

    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    public ActionResult<IEnumerable<UserDto>> GetUsers([FromQuery] int? pageNumber, [FromQuery] int? pageSize)
    {
        var number = Math.Max(pageNumber ?? 1, 1);
        var size = Math.Clamp(pageSize ?? 10, 1, 20);

        var page = _userRepository.GetPage(number, size);
        AddPaginationHeader(page, number, size);

        return Ok(_mapper.Map<IEnumerable<UserDto>>(page));
    }

    [HttpOptions]
    public IActionResult GetUsersOptions()
    {
        Response.Headers.Append("Allow", "GET, POST, OPTIONS");
        return Ok();
    }

    private void AddPaginationHeader(PageList<UserEntity> page, int pageNumber, int size)
    {
        var paginationHeader = new
        {
            previousPageLink = page.HasPrevious ? BuildPageLink(pageNumber - 1, size) : null,
            nextPageLink = page.HasNext ? BuildPageLink(pageNumber + 1, size) : null,
            totalCount = page.TotalCount,
            pageSize = page.PageSize,
            currentPage = page.CurrentPage,
            totalPages = page.TotalPages
        };
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
    }

    private string? BuildPageLink(int pageNumber, int pageSize)
    {
        return _linkGenerator.GetUriByRouteValues(
            HttpContext,
            nameof(GetUsers),
            new { pageNumber, pageSize }
        );
    }
}