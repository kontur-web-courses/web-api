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
    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;
    private readonly LinkGenerator linkGenerator;

    public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
        this.linkGenerator = linkGenerator;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
        {
            return NotFound();
        }
        var userDto = mapper.Map<UserDto>(user);
        
        return Ok(userDto);
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] UserCreateDto userDto)
    {
        if (userDto == null)
        {
            return BadRequest();
        }
        
        if (!ModelState.IsValid)
        {
            ModelState.AddModelError("Login", "Логин должен быть");
            return UnprocessableEntity(ModelState);
        }

        if (userDto.Login.Any(c => !char.IsLetterOrDigit(c)))
        {
            ModelState.AddModelError("Login", "Логин должен состоять только из цифр и букв");
            return UnprocessableEntity(ModelState);
        }
        
        var user = mapper.Map<UserEntity>(userDto);
        var createdUserEntity = userRepository.Insert(user);
        
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UserPutDto userDto)
    {
        if (userId == Guid.Empty || userDto == null)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        userDto.Id = userId;

        var userFromDto = mapper.Map<UserEntity>(userDto);
        userRepository.UpdateOrInsert(userFromDto, out var isCreated);

        if (isCreated) 
        {
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userFromDto.Id },
                userFromDto.Id);
        }

        return NoContent();
    }
    
    [HttpPatch("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser ([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserPatchDto> patchDoc)
    {
        if (patchDoc == null)
        {
            return BadRequest();
        }

        if (userId == Guid.Empty)
        {
            return NotFound();
        }
        
        var userDto = new UserPatchDto();
        patchDoc.ApplyTo(userDto, ModelState);

        if (!TryValidateModel(userDto))
        {
            return UnprocessableEntity(ModelState);
        }
        
        var userFromDb = userRepository.FindById(userId);
        if (userFromDb == null)
        {
            return NotFound();
        }
        
        var user = mapper.Map(userDto, userFromDb);
        userRepository.UpdateOrInsert(user, out _);

        return NoContent();
    }

    [HttpDelete("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return NotFound();
        }
        
        var userFromDb = userRepository.FindById(userId);
        if (userFromDb == null)
        {
            return NotFound();
        }
        
        userRepository.Delete(userId);
        
        return NoContent();
    }
    
    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserByIdHead([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
        {
            return NotFound();
        }
        
        Response.Headers.Append("Content-Type", "application/json; charset=utf-8");
        
        return Ok();
    }
    
    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    public ActionResult<IEnumerable<UserDto>> GetUsers([FromQuery] int pageNumber = 1,  [FromQuery] int pageSize = 10)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Min(Math.Max(1, pageSize), 20);
        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);
        var paginationHeader = GetPaginationHeader(pageList);
        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        
        return Ok(users);
    }
    
    [HttpOptions]
    [Produces("application/json", "application/xml")]
    public ActionResult GetOptions()
    {
        Response.Headers.Add("Allow", "GET, POST, OPTIONS");
        return Ok();
    }

    private object GetPaginationHeader(PageList<UserEntity> pageList)
    {
        var paginationHeader = new
        {
            previousPageLink = pageList.HasPrevious
                ? linkGenerator.GetUriByRouteValues(HttpContext, "GetUsers", new
                {
                    pageSize = pageList.PageSize,
                    pageNumber = pageList.CurrentPage - 1

                })
                : null,
            nextPageLink = pageList.HasNext
                ? linkGenerator.GetUriByRouteValues(HttpContext, "GetUsers", new
                {
                    pageSize = pageList.PageSize + 1 == pageList.TotalPages ? pageList.TotalCount - pageList.CurrentPage * pageList.PageSize : pageList.PageSize,
                    pageNumber = pageList.CurrentPage + 1
                })
                : null,
            totalCount = pageList.TotalCount,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages,
        };
        return paginationHeader;
    }
}