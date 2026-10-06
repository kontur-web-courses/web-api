using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 20;

    private static readonly JsonSerializerSettings PaginationSerializerSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;
    private readonly LinkGenerator linkGenerator;

    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        this.userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        this.linkGenerator = linkGenerator ?? throw new ArgumentNullException(nameof(linkGenerator));
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] string userId)
    {
        var userResult = TryGetExistingUser(userId, NotFound, out var userEntity, out var errorResult);
        if (!userResult)
            return (ActionResult)errorResult!;

        var userDto = mapper.Map<UserDto>(userEntity);
        return Ok(userDto);
    }

    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult HeadUserById([FromRoute] string userId)
    {
        var ok = TryGetExistingUser(userId, NotFound, out var _, out var errorResult);
        if (!ok)
            return errorResult!;
        
        return Content(string.Empty, "application/json; charset=utf-8");
    }

    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    public ActionResult<IEnumerable<UserDto>> GetUsers([FromQuery] int? pageNumber, [FromQuery] int? pageSize)
    {
        var actualPageNumber = pageNumber.HasValue ? Math.Max(pageNumber.Value, DefaultPageNumber) : DefaultPageNumber;
        var actualPageSize = pageSize.HasValue ? Math.Clamp(pageSize.Value, 1, MaxPageSize) : DefaultPageSize;

        var pageList = userRepository.GetPage(actualPageNumber, actualPageSize);
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);

        AddPaginationHeader(pageList, actualPageNumber, actualPageSize);

        return Ok(users);
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] CreateUserDto? user)
    {
        if (user is null)
            return BadRequest();

        ValidateLoginCharacters(user.Login);

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = mapper.Map<UserEntity>(user);
        var createdUser = userRepository.Insert(userEntity);

        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUser.Id },
            createdUser.Id);
    }

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromRoute] string userId, [FromBody] UpdateUserDto? user)
    {
        if (!Guid.TryParse(userId, out var id))
            return BadRequest();

        if (user is null)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var entityToUpdate = mapper.Map(user, new UserEntity(id));
        userRepository.UpdateOrInsert(entityToUpdate, out var isInserted);

        if (isInserted)
        {
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = entityToUpdate.Id },
                entityToUpdate.Id);
        }

        return NoContent();
    }

    [HttpPatch("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser(
        [FromRoute] string userId,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]
        JsonPatchDocument<UpdateUserDto>? patchDoc)
    {
        // 1) Пустое тело -> 400 (тест этого и ждёт)
        if (patchDoc is null)
            return BadRequest();

        // 2) Теперь ищем пользователя
        var userResult = TryGetExistingUser(userId, NotFound, out var existingUser, out var errorResult);
        if (!userResult)
            return errorResult!;

        var userToPatch = mapper.Map<UpdateUserDto>(existingUser);
        patchDoc.ApplyTo(userToPatch, ModelState);

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        if (!TryValidateModel(userToPatch))
            return UnprocessableEntity(ModelState);

        mapper.Map(userToPatch, existingUser);
        userRepository.Update(existingUser);

        return NoContent();
    }


    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] string userId)
    {
        var userResult = TryGetExistingUser(userId, NotFound, out var userEntity, out var errorResult);
        if (!userResult)
            return errorResult!;

        userRepository.Delete(userEntity.Id);
        return NoContent();
    }

    [HttpOptions]
    public IActionResult GetUsersOptions()
    {
        Response.Headers["Allow"] = new[] { "GET", "POST", "OPTIONS" };
        return Ok();
    }

    private void ValidateLoginCharacters(string? login)
    {
        if (string.IsNullOrEmpty(login))
            return;

        if (login.Any(ch => !char.IsLetterOrDigit(ch)))
            ModelState.AddModelError("Login", "Login should contain only letters or digits");
    }

    private bool TryGetExistingUser(string userId, Func<IActionResult> invalidIdResultFactory, out UserEntity? userEntity, out IActionResult? errorResult)
    {
        userEntity = null;
        errorResult = null;

        if (!Guid.TryParse(userId, out var id))
        {
            errorResult = invalidIdResultFactory();
            return false;
        }

        var entity = userRepository.FindById(id);
        if (entity is null)
        {
            errorResult = NotFound();
            return false;
        }

        userEntity = entity;
        return true;
    }

    private void AddPaginationHeader(PageList<UserEntity> pageList, int pageNumber, int pageSize)
    {
        var paginationHeader = new
        {
            previousPageLink = pageList.HasPrevious ? BuildUsersPageLink(pageNumber - 1, pageSize) : null,
            nextPageLink = pageList.HasNext ? BuildUsersPageLink(pageNumber + 1, pageSize) : null,
            totalCount = pageList.TotalCount,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages
        };

        Response.Headers["X-Pagination"] = JsonConvert.SerializeObject(paginationHeader, PaginationSerializerSettings);
    }

    private string? BuildUsersPageLink(int pageNumber, int pageSize)
    {
        return linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber, pageSize });
    }
}