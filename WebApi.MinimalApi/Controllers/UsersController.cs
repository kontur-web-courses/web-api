using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json", "application/xml")]
public class UsersController : Controller
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

    [HttpHead("{userId}")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user != null)
        {
            var userDto = _mapper.Map<UserDto>(user);
            return HttpMethods.IsHead(Request.Method)
                ? new ContentResult { StatusCode = 200, ContentType = "application/json; charset=utf-8"}
                : Ok(userDto);
        }
        
        return NotFound();
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] UserForCreateDto? user)
    {
        if (user == null)
        {
            return BadRequest();
        }
        
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        if (!user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("Login", "Login should contain only letters or digits");
            return UnprocessableEntity(ModelState);
        }
        
        var userEntity = _mapper.Map<UserEntity>(user);
        var createdUserEntity = _userRepository.Insert(userEntity);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }

    [HttpPut("{userId}")]
    public IActionResult PutUser([FromRoute] Guid userId, [FromBody] UserForPutDto? user)
    {
        if (user == null || userId == Guid.Empty)
        {
            return BadRequest();
        }
        
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = new UserEntity(userId);
        _mapper.Map(user, userEntity);
        _userRepository.UpdateOrInsert(userEntity, out var isInserted);

        if (isInserted)
        {
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id);
        }
        
        return NoContent();
    }

    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser(
        [FromRoute] Guid userId,
        [FromBody] JsonPatchDocument<UserToUpdateDto>? patchDoc)
    {
        if (userId == Guid.Empty)
        {
            return NotFound();
        }
        
        var user = _userRepository.FindById(userId);
        
        if (patchDoc == null)
            return BadRequest();
        
        if (user == null) 
            return NotFound();
        
        var userDto = new UserToUpdateDto();
        _mapper.Map(user, userDto);
        patchDoc.ApplyTo(userDto, ModelState);
        if (!ModelState.IsValid || !TryValidateModel(userDto))
            return UnprocessableEntity(ModelState);
        
        
        var userEntity = new UserEntity(userId);
        _mapper.Map(user, userEntity);
        _userRepository.Update(userEntity);

        return NoContent();
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (userId == Guid.Empty || _userRepository.FindById(userId) == null)
        {
            return NotFound();
        }

        _userRepository.Delete(userId);

        return NoContent();
    }

    [HttpGet(Name = nameof(GetAllUsers))]
    public IActionResult GetAllUsers([FromQuery] PaginationParameters paginationParameters)
    {
        var (pageNumber, pageSize) = (paginationParameters.PageNumber, paginationParameters.PageSize);

        if (!ModelState.IsValid)
        {
            (pageNumber, pageSize) = (Math.Max(1, pageNumber), Math.Max(1, Math.Min(20, pageSize)));
        }
        
        var pageList = _userRepository.GetPage(pageNumber, pageSize);
        var users = _mapper.Map<IEnumerable<UserDto>>(pageList);

        var generateLink = new Func<int, string?>(pn =>
            _linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetAllUsers), new { pageNumber = pn, pageSize}));
        
        var paginationHeader = new
        {
            previousPageLink = pageList.HasPrevious ? generateLink(pageNumber - 1) : null,
            nextPageLink = pageList.HasNext ? generateLink(pageNumber + 1) : null,
            totalCount = pageList.TotalCount,
            pageSize = pageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages,
        };
        
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

        return Ok(users);
    }
}