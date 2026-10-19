using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    IUserRepository UserRepository { get; set; }
    IMapper mapper;
    LinkGenerator linkGenerator;
    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        UserRepository = userRepository;
        this.mapper = mapper;
        this.linkGenerator = linkGenerator;
    }

    [HttpHead("{userId}")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        
        var user = UserRepository.FindById(userId);
        if (user == null)
        {
            return NotFound();
        }
        if (HttpMethods.IsHead(Request.Method))
        {
            Response.Headers.ContentType = "application/json; charset=utf-8";
            return Ok();
        }
            
        return Ok(mapper.Map<UserDto>(user));
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] CreateUserDto user)
    {
        if (user == null)
        {
            return BadRequest();
        }

        var newUser = mapper.Map<UserEntity>(user);
        
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        foreach (var character in user.Login)
        {
            if (!char.IsLetterOrDigit(character))
            {
                ModelState.AddModelError("Login", "incorrect login");
                break;
            }
        }
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var savedUser = UserRepository.Insert(newUser);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = savedUser.Id },
            mapper.Map<UserDto>(savedUser).Id);
    }

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdateUserDto updateUserDto)
    {
        if (userId == Guid.Empty || updateUserDto is null)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var userEntity = new UserEntity(userId);
        mapper.Map(updateUserDto, userEntity);
        UserRepository.UpdateOrInsert(userEntity, out var isInserted);
        if (isInserted)
            return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = userEntity.Id },
            mapper.Map<UserDto>(userEntity).Id);
        return NoContent();
        
    }

    [HttpPatch("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc)
    {
        if (patchDoc is null)
            return BadRequest();
        
        var user = UserRepository.FindById(userId);
        if (user == null || userId == Guid.Empty)
        {
            return NotFound();
        }
        var updateUserDto = new UpdateUserDto();
        patchDoc.ApplyTo(updateUserDto, ModelState);
        TryValidateModel(updateUserDto);
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var userEntity = new UserEntity(userId);
        mapper.Map(updateUserDto, userEntity);
        UserRepository.Update(userEntity);
        return NoContent();

    }

    [HttpDelete("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var user = UserRepository.FindById(userId);
        if (user == null || userId == Guid.Empty)
        {
            return NotFound();
        }

        UserRepository.Delete(userId);
        return NoContent();
    }

    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUsers([FromQuery] UserPaginationParameters userPaginationParameters)
    {

        var pageList = UserRepository.GetPage(userPaginationParameters.PageNumber, userPaginationParameters.PageSize);
        var totalPages = (int)Math.Ceiling((double)pageList.TotalCount / userPaginationParameters.PageSize);

        var previousPageLink = userPaginationParameters.PageNumber > 1
        ? linkGenerator.GetUriByRouteValues(
            HttpContext, 
            nameof(GetUsers), 
            new { pageNumber = userPaginationParameters.PageNumber - 1, 
                pageSize = userPaginationParameters.PageSize })
        : null;

        var nextPageLink = userPaginationParameters.PageNumber < totalPages
        ? linkGenerator.GetUriByRouteValues(
            HttpContext, 
            nameof(GetUsers), 
            new { pageNumber = userPaginationParameters.PageNumber + 1,
                pageSize = userPaginationParameters.PageSize })
        : null;

        var paginationHeader = new
        {
            previousPageLink,
            nextPageLink,
            totalCount = pageList.TotalCount,
            pageSize = userPaginationParameters.PageSize,
            currentPage = userPaginationParameters.PageNumber,
            totalPages
        };
        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);
        return Ok(users);
    }

    [HttpOptions]
    public IActionResult GetUsersOptions()
    {
        Response.Headers.Append("Allow", "GET, POST, OPTIONS");

        return Ok();
    }


    public class UserPaginationParameters
    {
        private const int MaxPageSize = 20;
        private int _pageNumber = 1;
        private int _pageSize = 10;

        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1 ? 1 : (value > MaxPageSize ? MaxPageSize : value);
        }
    }

}