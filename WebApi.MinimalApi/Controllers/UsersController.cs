using System.Security.Cryptography.X509Certificates;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NUnit.Framework;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    private IUserRepository Repository;
    private IMapper Mapper;
    private readonly LinkGenerator linkGenerator;
    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        Repository = userRepository;
        Mapper = mapper;
        this.linkGenerator = linkGenerator;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [HttpHead("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public IActionResult GetUserById([FromRoute] Guid userId)
    {
        var user = Repository.FindById(userId);
        if (user is null)
            return NotFound();

        var result = Mapper.Map<UserDto>(user);

        if (HttpContext.Request.Method != "HEAD") 
            return Ok(result);
        
        Response.ContentType = "application/json; charset=utf-8";
        return Ok();

    }


    [HttpPost]
    public IActionResult CreateUser([FromBody] AddUserDto user)
    {
        var createdUserEntity = Mapper.Map<UserEntity>(user);
        if (createdUserEntity is null)
            return BadRequest();
        if(createdUserEntity.Login?.All(char.IsLetterOrDigit) == false)
            ModelState.AddModelError(nameof(createdUserEntity.Login).ToLower(), "Login must be letters or digits");
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var createdUser = Repository.Insert(createdUserEntity);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUser.Id },
            createdUser.Id);
    }

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser(Guid userId, [FromBody] UpdateUserDto user)
    {
        if (user == null || userId == Guid.Empty)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = Mapper.Map(user, new UserEntity(userId));

        Repository.UpdateOrInsert(userEntity, out var isInsert);

        if (isInsert)
            return CreatedAtRoute(
                nameof(GetUserById),
                new {userId = userId},
                userId);
        return NoContent();
    }
    
        
    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser([FromBody] JsonPatchDocument<UpdateUserDto> patchDoc, Guid userId)
    {
        if (patchDoc == null)
            return BadRequest();
    
        var user = Repository.FindById(userId);
        if (user == null || userId == Guid.Empty)
            return NotFound();

        var updateUserDto = Mapper.Map(user, new UpdateUserDto());

        patchDoc.ApplyTo(updateUserDto, ModelState);

        TryValidateModel(updateUserDto);

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        Mapper.Map(updateUserDto, user);

        Repository.Update(user);

        return NoContent();
    }
    
    [HttpDelete("{userId}")]
    public IActionResult DeleteUser(Guid userId)
    {
        var user = Repository.FindById(userId);
        if (user == null)
        {
            return NotFound();
        }

        Repository.Delete(userId);

        return NoContent();
    }
    
    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    public IActionResult GetUsers(int pageNumber = 1, int pageSize = 10)
    {
        if (pageNumber < 1)
            pageNumber = 1;
        if (pageSize < 1)
            pageSize = 1;
        if (pageSize > 20)
            pageSize = 20;
        
        var pageList = Repository.GetPage(pageNumber, pageSize);
        var users = Mapper.Map<IEnumerable<UserDto>>(pageList);
        
        var paginationHeader = new
        {
            previousPageLink = pageList.HasPrevious ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber - 1, pageSize }) : null,
            nextPageLink = pageList.HasNext ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber + 1, pageSize }) : null,
            totalCount = pageList.TotalCount,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages
        };

        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

        return Ok(users);
    }
    
    [HttpOptions(Name = nameof(GetUsersOptions))]
    public IActionResult GetUsersOptions()
    {
        Response.Headers.Add("Allow", "POST, GET, OPTIONS");
    
        return Ok();
    }
}