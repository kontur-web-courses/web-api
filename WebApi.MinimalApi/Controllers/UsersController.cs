using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json", "application/xml")]
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

    [HttpGet("{userId:guid}", Name = nameof(GetUserById))]
    [HttpHead("{userId:guid}")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var userEntity = userRepository.FindById(userId);
        
        if (userEntity == null)
        {
            return NotFound();
        }
        
        if (Request.Method == HttpMethods.Head)
        {
            Response.Headers.ContentType = "application/json; charset=utf-8";
            return Ok();
        }
        
        return Ok(mapper.Map<UserDto>(userEntity));
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] UserCreateDto? userCreate)
    {
        if (userCreate == null)
            return BadRequest();

        if (string.IsNullOrEmpty(userCreate.Login) || !userCreate.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("Login", "invalid login");
            return UnprocessableEntity(ModelState);
        }

        var userEntity = mapper.Map<UserEntity>(userCreate);
        var user = userRepository.Insert(userEntity);

        return CreatedAtAction(
            nameof(GetUserById),
            new { userId = user.Id },
            user.Id);
    }
    
    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UserUpdateDto? userUpdateDto)
    {
        if (userUpdateDto == null || userId == Guid.Empty)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var user = mapper.Map(userUpdateDto, new UserEntity(userId));

        userRepository.UpdateOrInsert(user, out var isNew);

        if (isNew)
        {
            return CreatedAtAction(
                nameof(GetUserById),
                new { userId = user.Id },
                user.Id);
        }

        return NoContent();
    }

    [HttpPatch("{userId:guid}")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserUpdateDto>? patchDocument)
    {
        if (patchDocument == null)
        {
            return BadRequest();
        }

        var user = userRepository.FindById(userId);
        if (user == null || userId == Guid.Empty)
        {
            return NotFound();
        }

        var userToUpdate = mapper.Map<UserUpdateDto>(user);
        patchDocument.ApplyTo(userToUpdate, ModelState);

        if (!TryValidateModel(userToUpdate))
        {
            return UnprocessableEntity(ModelState);
        }

        var updatedEntity = mapper.Map(userToUpdate, user);
        userRepository.Update(updatedEntity);

        return NoContent();
    }

    [HttpDelete("{userId:guid}")]
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

    [HttpGet]
    public ActionResult<IEnumerable<UserDto>> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 20);

        var userPage = userRepository.GetPage(pageNumber, pageSize);
        var usersDtos = mapper.Map<IEnumerable<UserDto>>(userPage);

        var paginationMetadata = new
        {
            previousPageLink = userPage.HasPrevious ? 
                linkGenerator.GetUriByAction(HttpContext, nameof(GetUsers), values: new 
                { 
                    pageNumber = pageNumber - 1, 
                    pageSize 
                }) : null,
            nextPageLink = userPage.HasNext ? 
                linkGenerator.GetUriByAction(HttpContext, nameof(GetUsers), values: new 
                { 
                    pageNumber = pageNumber + 1, 
                    pageSize 
                }) : null,
            totalCount = userPage.TotalCount,
            pageSize,
            currentPage = pageNumber,
            totalPages = (int)Math.Ceiling(userPage.TotalCount / (double)pageSize)
        };

        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationMetadata));

        return Ok(usersDtos);
    }

    [HttpOptions]
    public IActionResult GetOptions()
    {
        Response.Headers.Append("Allow", "GET, POST, OPTIONS");
        return Ok();
    }
}