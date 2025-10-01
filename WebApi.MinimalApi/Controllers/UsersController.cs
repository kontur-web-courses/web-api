using System.ComponentModel.DataAnnotations;
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
public partial class UsersController : Controller
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

    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUsers(
        [FromQuery] int pageNumber = 1, 
        [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1)
            pageNumber = 1;
        if (pageSize < 1)
            pageSize = 1;
        if (pageSize > 20)
            pageSize = 20;
        
        var pageList = _userRepository.GetPage(pageNumber, pageSize);
        var users = _mapper.Map<IEnumerable<UserDto>>(pageList);
        var paginationHeader = new
        {
            previousPageLink = pageNumber > 1 ? _linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new {pageNumber = pageNumber - 1, pageSize}) : null,
            nextPageLink = pageNumber < pageList.TotalPages ? _linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new {pageNumber = pageNumber + 1, pageSize}) : null,
            totalCount = pageList.TotalCount,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages
        };
        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));


        return Ok(users);
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        
        if (user is null)
            return NotFound();

        if (!HttpMethods.IsHead(Request.Method))
            return _mapper.Map<UserEntity, UserDto>(user);
        
        if (Request.Headers.Accept == "*/*")
            Response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        Response.Headers.Add("Content-Type", $"{Request.Headers.Accept}; charset=utf-8");
        return Ok();

    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] CreateUserRequest request)
    {
        if (request is null)
            return BadRequest();
        
        if (request.Login is not null && !Regex.IsMatch(request.Login, "^[a-zA-Z0-9]*$"))
            ModelState.AddModelError(nameof(CreateUserRequest.Login), "Низя(");
        
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        
        var user = _mapper.Map<CreateUserRequest, UserEntity>(request);
        var createdUser = _userRepository.Insert(user);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUser.Id },
            createdUser.Id);
    }

    [HttpPut("{userId}")]
    public IActionResult UpdateUser(Guid userId, [FromBody] UpdateUserRequest request)
    {
        if (userId == default || request is null)
            return BadRequest();
        
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var user = _mapper.Map(request, _userRepository.FindById(userId) ?? new UserEntity(userId));
        _userRepository.UpdateOrInsert(user, out var isCreated);

        if (isCreated)
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId },
                userId);
        return NoContent();
    }
    
    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser (Guid userId, [FromBody] JsonPatchDocument<UpdateUserRequest> patchDoc)
    {
        if (patchDoc is null)
            return BadRequest();
        if (userId == default)
            return NotFound();

        var user = _userRepository.FindById(userId);
        if (user is null)
            return NotFound();
        
        var updateDto = _mapper.Map<UserEntity, UpdateUserRequest>(user);
        
        patchDoc.ApplyTo(updateDto, ModelState);
        TryValidateModel(updateDto);
        
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var editedUser = _mapper.Map(updateDto, user);

        _userRepository.Update(editedUser);

        return NoContent();
    }
    
    [HttpDelete("{userId}")]
    public IActionResult DeleteUser (Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user is null)
            return NotFound();

        _userRepository.Delete(userId);
        return NoContent();
    }

    [HttpOptions]
    public IActionResult OptionsLastUuuuhu()
    {
        Response.Headers.Add("Allow", "OPTIONS,GET,POST");
        return Ok();
    }
}