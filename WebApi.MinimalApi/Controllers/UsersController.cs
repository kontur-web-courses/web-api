using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
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

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user == null)
            return NotFound();

        var userDto = _mapper.Map<UserDto>(user);
        
        return Ok(userDto);
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] CreateUserDto user)
    {
        // Проверка на пустой контент
        if (user == null)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        // Проверка логина на буквы и цифры
        if (!string.IsNullOrEmpty(user.Login) && !user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("Login", "Login should contain only letters or digits");
            return UnprocessableEntity(ModelState);
        }

        var userEntity = _mapper.Map<UserEntity>(user);
        var createdUser = _userRepository.Insert(userEntity);
        var userDto = _mapper.Map<UserDto>(createdUser);

        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUser.Id },
            createdUser.Id);
    }

    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdateUserDto user)
    {
        // Проверка на пустой контент
        if (user == null)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            // Если есть ошибки валидации userId (некорректный Guid), возвращаем BadRequest
            if (ModelState.ContainsKey("userId") && ModelState["userId"].Errors.Any())
                return BadRequest();
            // Для ошибок валидации полей возвращаем UnprocessableEntity
            return UnprocessableEntity(ModelState);
        }

        var existingUser = _userRepository.FindById(userId);
        if (existingUser == null)
        {
            // Upsert - создаем нового пользователя
            var newUser = new UserEntity(userId, user.Login, user.LastName, user.FirstName, 0, null);
            _userRepository.UpdateOrInsert(newUser, out _);
            var userDto = _mapper.Map<UserDto>(newUser);
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = newUser.Id },
                userDto);
        }
        else
        {
            // Update - обновляем существующего
            _mapper.Map(user, existingUser);
            _userRepository.Update(existingUser);
            return NoContent();
        }
    }

    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc)
    {
        // Проверка на пустой контент
        if (patchDoc == null)
            return BadRequest();

        var existingUser = _userRepository.FindById(userId);
        if (existingUser == null)
            return NotFound();

        var updateDto = _mapper.Map<UpdateUserDto>(existingUser);
        patchDoc.ApplyTo(updateDto, ModelState);

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        TryValidateModel(updateDto);
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        _mapper.Map(updateDto, existingUser);
        _userRepository.Update(existingUser);
        return NoContent();
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user == null)
            return NotFound();

        _userRepository.Delete(userId);
        return NoContent();
    }

    [HttpHead("{userId}")]
    public IActionResult HeadUserById([FromRoute] Guid userId)
    {
        var user = _userRepository.FindById(userId);
        if (user == null)
            return NotFound();

        Response.Headers["Content-Type"] = "application/json; charset=utf-8";
        return Ok();
    }

    [HttpGet(Name = "GetUsers")]
    public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        // Ограничения параметров
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 20) pageSize = 20;

        var pageList = _userRepository.GetPage(pageNumber, pageSize);
        var users = _mapper.Map<IEnumerable<UserDto>>(pageList);

        // Добавление заголовка пагинации
        var paginationHeader = new
        {
            previousPageLink = pageList.HasPrevious ? 
                _linkGenerator.GetUriByRouteValues(HttpContext, "GetUsers", new { pageNumber = pageNumber - 1, pageSize }) : null,
            nextPageLink = pageList.HasNext ? 
                _linkGenerator.GetUriByRouteValues(HttpContext, "GetUsers", new { pageNumber = pageNumber + 1, pageSize }) : null,
            totalCount = pageList.TotalCount,
            pageSize = pageList.PageSize,
            currentPage = pageList.CurrentPage,
            totalPages = pageList.TotalPages,
        };
        Response.Headers["X-Pagination"] = JsonConvert.SerializeObject(paginationHeader);

        return Ok(users);
    }

    [HttpOptions]
    public IActionResult GetUsersOptions()
    {
        Response.Headers["Allow"] = "GET, POST, OPTIONS";
        return Ok();
    }
}