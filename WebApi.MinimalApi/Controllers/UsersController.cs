using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
        var possibleUser = userRepository.FindById(userId);

        if (possibleUser == null)
            return NotFound();

        if (HttpContext.Request.Method == HttpMethods.Head)
        {
            HttpContext.Response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            return Ok();
        }

        var userDto = mapper.Map<UserDto>(possibleUser);
        return Ok(userDto);
    }

    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    public ActionResult<IEnumerable<UserDto>> GetUsers(
        [FromQuery] [Range(1, int.MaxValue)] [DefaultValue(1)]
        int pageNumber,
        [FromQuery] [Range(1, 20)] [DefaultValue(10)]
        int pageSize)
    {
        if (ModelState.GetFieldValidationState("pageNumber") == ModelValidationState.Invalid) pageNumber = 1;

        if (ModelState.GetFieldValidationState("pageSize") == ModelValidationState.Invalid)
        {
            if (pageSize < 1) pageSize = 1;
            else if (pageSize > 20) pageSize = 20;
        }


        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);

        var paginationHeader = new
        {
            previousPageLink = pageList.CurrentPage == 1
                ? null
                : linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber - 1, pageSize }),
            nextPageLink = pageList.CurrentPage == pageList.TotalPages
                ? null
                : linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber + 1, pageSize }),
            totalCount = pageList.TotalCount,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages,
        };

        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

        return Ok(users);
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] CreateUserDto user)
    {
        if (user == null)
            return BadRequest();
        if (user.Login == null)
        {
            return UnprocessableEntity(ModelState);
        }

        if (user.Login != null && !user.Login.All(char.IsLetterOrDigit))
            ModelState.AddModelError("Login", "Login must contain only characters or digits");

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
    public IActionResult UpdateUser([FromRoute] Guid userId, UpdateUserDto user)
    {
        if (user == null || userId == Guid.Empty)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = mapper.Map(user, new UserEntity(userId));

        bool isInsert;
        userRepository.UpdateOrInsert(userEntity, out isInsert);

        if (isInsert)
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userId },
                userId);
        return NoContent();
    }

    [HttpPatch("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc)
    {
        if (patchDoc == null)
            return BadRequest();

        var user = userRepository.FindById(userId);

        if (user == null)
            return NotFound();

        var updateUserDto = mapper.Map<UpdateUserDto>(user);

        patchDoc.ApplyTo(updateUserDto, ModelState);

        TryValidateModel(updateUserDto);

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        return NoContent();
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);

        if (user == null)
        {
            return NotFound();
        }

        userRepository.Delete(userId);
        return NoContent();
    }

    [HttpOptions]
    public IActionResult GetAllMethods()
    {
        Response.Headers.Add("Allow", new[] { "GET", "POST", "OPTIONS" });

        return Ok();
    }
}

