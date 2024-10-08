using System.ComponentModel.DataAnnotations;
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
    private readonly IMapper mapper;
    private readonly IUserRepository userRepository;
    private readonly LinkGenerator LinkGenerator;

    public UsersController(IUserRepository userRepository,
                           IMapper mapper,
                           LinkGenerator linkGenerator)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
        LinkGenerator = linkGenerator;
    }
    
    [HttpHead("{userId}")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();
        if (HttpMethods.IsHead(HttpContext.Request.Method))
            Response.Body = Stream.Null;
        var userDto = mapper.Map<UserDto>(user);
        return Ok(userDto);
    }

    [HttpPost("")]
    public IActionResult CreateUser([FromBody] UserToCreateDto? user)
    {
        if (user == null)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var userEntity = mapper.Map<UserEntity>(user);
        userEntity = userRepository.Insert(userEntity);

        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = userEntity.Id },
            userEntity.Id);
    }

    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromRoute] Guid? userId, [FromBody] UserToUpdateDto? user)
    {
        if (user == null || ModelState.IsInvalid(nameof(userId)))
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        userId ??= Guid.NewGuid();
        var mappedUser = mapper.Map(user, new UserEntity(userId.Value));
        userRepository.UpdateOrInsert(mappedUser, out var inserted);
        return inserted
            ? CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userId.Value },
                mappedUser.Id)
            : NoContent();
    }

    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserToUpdateDto>? patchDocument)
    {
        if (patchDocument == null)
            return BadRequest();
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        var userToUpdate = mapper.Map<UserToUpdateDto>(user);
        patchDocument.ApplyTo(userToUpdate, ModelState);
        if (!TryValidateModel(userToUpdate))
            return UnprocessableEntity(ModelState);
        mapper.Map(userToUpdate, user);
        userRepository.Update(user);
        return NoContent();
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (!ModelState.IsValid)
            return NotFound();
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();
        userRepository.Delete(userId);
        return NoContent();
    }

    [HttpGet(Name = nameof(GetUsers))]
    public ActionResult<IEnumerable<UserDto>> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 20);
        var users = userRepository.GetPage(pageNumber, pageSize);
        var previousPageLink = users.CurrentPage == 1 ? null : LinkGenerator
            .GetUriByRouteValues(HttpContext,
                                 nameof(GetUsers),
                                 new
                                 {
                                     pageNumber = users.CurrentPage - 1,
                                     pageSize = users.PageSize
                                 });
        var nextPageLink = users.CurrentPage >= 20 ? null : LinkGenerator
            .GetUriByRouteValues(HttpContext,
                                 nameof(GetUsers),
                                 new
                                 {
                                     pageNumber = users.CurrentPage + 1,
                                     pageSize = users.PageSize
                                 });
        var paginationHeader = new
        {
            previousPageLink,
            nextPageLink,
            totalCount = users.TotalCount,
            pageSize = users.PageSize,
            currentPage = users.CurrentPage,
            totalPages = users.TotalPages
        };
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        return Ok(users);
    }

    [HttpOptions]
    public IActionResult UsersOptions()
    {
        var allowedMethods = new[] { HttpMethods.Post, HttpMethods.Get, HttpMethods.Options };
        Response.Headers.AppendList(HeaderNames.Allow, allowedMethods);
        return Ok();
    }
}