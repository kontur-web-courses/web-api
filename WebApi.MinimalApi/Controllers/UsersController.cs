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

    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
        this.linkGenerator = linkGenerator;
    }

    [HttpHead("{userId}")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();

        if (HttpMethods.IsHead(Request.Method))
        {
            Response.ContentType = "application/json; charset=utf-8";
            return Ok();
        }

        return Ok(mapper.Map<UserDto>(user));
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] CreateUserDto user)
    {
        if (user == null)
            return BadRequest();
        if (!string.IsNullOrEmpty(user.Login) && !user.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError("Login", "Login should contain only letters or digits");
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var createdUserEntity = userRepository.Insert(mapper.Map<UserEntity>(user));
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdateUserDto user)
    {
        if (user == null || userId == Guid.Empty)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var entity = mapper.Map(user, new UserEntity(userId));
        userRepository.UpdateOrInsert(entity, out var isInserted);
        if (isInserted)
            return CreatedAtRoute(nameof(GetUserById), new { userId = entity.Id }, entity.Id);
        return NoContent();
    }

    [HttpPatch("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserDto> patchDocument)
    {
        if (patchDocument == null)
            return BadRequest();
        var entity = userRepository.FindById(userId);
        if (entity == null)
            return NotFound();
        var updateDto = mapper.Map<UpdateUserDto>(entity);
        patchDocument.ApplyTo(updateDto, ModelState);
        TryValidateModel(updateDto);
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        mapper.Map(updateDto, entity);
        userRepository.Update(entity);
        return NoContent();
    }

    [HttpDelete("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var userEntity = userRepository.FindById(userId);
        if (userEntity == null)
            return NotFound();

        userRepository.Delete(userId);
        return NoContent();
    }


    [HttpGet("", Name = nameof(GetUsersPaginationDto))]
    [Produces("application/json", "application/xml")]
    public IActionResult GetAllUsers([FromQuery] GetUsersPaginationDto paginationDto)
    {
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var pageList = userRepository.GetPage(paginationDto.PageNumber, paginationDto.PageSize);
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);

        var previousPageLink = linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsersPaginationDto),
            new GetUsersPaginationDto
            {
                PageNumber = paginationDto.PageNumber - 1,
                PageSize = paginationDto.PageSize
            });

        var nextPageLink = linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsersPaginationDto),
            new GetUsersPaginationDto
            {
                PageNumber = paginationDto.PageNumber + 1,
                PageSize = paginationDto.PageSize
            });

        var paginationHeader = new
        {
            previousPageLink = pageList.HasPrevious ? previousPageLink : null,
            nextPageLink = pageList.HasNext ? nextPageLink : null,
            totalCount = pageList.TotalCount,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages
        };
        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

        return Ok(users);
    }
}