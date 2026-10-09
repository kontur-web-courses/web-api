using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    IUserRepository userRepository;
    IMapper mapper;

    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();
        var isHead = HttpMethods.IsHead(Request.Method);
        if (isHead)
        {
            Response.ContentType = Request.Headers.Accept.ToString().Contains("xml")
            ? "application/xml; charset=utf-8"
            : "application/json; charset=utf-8";
            return Ok();
        }

        return Ok(mapper.Map<UserDto>(user));
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] NewUserDto? newUserDto)
    {
        if (newUserDto == null)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        if (!CheckKeyIsValid(newUserDto.Login))
        {
            ModelState.AddModelError(nameof(NewUserDto.Login), "Логин должен состоять из цифр и букв");
            return UnprocessableEntity(ModelState);
        }

        var createdUserEntity = mapper.Map<NewUserDto, UserEntity>(newUserDto);

        var insertedUser = userRepository.Insert(createdUserEntity);

        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = insertedUser.Id },
            insertedUser.Id);
    }

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] PutUserDto? putUserDto)
    {
        if (putUserDto == null || userId == Guid.Empty)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var existingUser = userRepository.FindById(userId);
        var userExistedBefore = existingUser != null;

        var userEntity = new UserEntity(userId); ;
        userEntity = mapper.Map(putUserDto, userEntity);

        userRepository.UpdateOrInsert(userEntity, out var isInserted);

        if (!userExistedBefore)
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id);

        return NoContent();
    }

    [HttpGet(Name = nameof(GetUsers))]
    [HttpHead]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 20);

        var pageList = userRepository.GetPage(pageNumber, pageSize);

        var previousPageLink = pageList.HasPrevious
            ? Url.Link(nameof(GetUsers), new { pageNumber = pageNumber - 1, pageSize })
            : null;

        var nextPageLink = pageList.HasNext
            ? Url.Link(nameof(GetUsers), new { pageNumber = pageNumber + 1, pageSize })
            : null;

        var paginationHeader = new
        {
            previousPageLink,
            nextPageLink,
            totalCount = pageList.TotalCount,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages
        };
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);
        return Ok(users);
    }

    private bool CheckKeyIsValid(string key)
    {
        foreach (var el in key)
        {
            if (!char.IsLetterOrDigit(el))
                return false;
        }
        return true;
    }
}