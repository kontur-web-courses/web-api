using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
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

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute, ModelBinder(typeof(UserIdModelBinder))] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
            return NotFound();

        if (HttpMethods.IsHead(Request.Method))
        {
            Response.ContentType = "application/json; charset=utf-8";
            return Ok();
        }

        var dto = mapper.Map<UserDto>(user);

        return Ok(dto);
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] UserToPostDto data)
    {
        if (data is null)
        {
            return BadRequest();
        }

        if (data.Login != null && !data.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("login", "Login must contain only letters and digits");
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var entity = mapper.Map<UserEntity>(data);
        var createdUser = userRepository.Insert(entity);

        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUser.Id },
            mapper.Map<UserDto>(createdUser));
    }

    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromRoute, ModelBinder(typeof(UserIdModelBinder))] Guid userId, [FromBody] UserToPutDto data)
    {
        if (data is null)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            if (ModelState.TryGetValue(nameof(userId), out var userIdState) && userIdState.Errors.Count > 0)
            {
                return BadRequest();
            }

            return UnprocessableEntity(ModelState);
        }

        var entity = mapper.Map(data, new UserEntity(userId));
        userRepository.UpdateOrInsert(entity, out var isInserted);

        if (isInserted)
        {
            return CreatedAtRoute(nameof(GetUserById), new { userId = entity.Id }, entity.Id);
        }

        return NoContent();
    }

    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser([FromRoute, ModelBinder(typeof(UserIdModelBinder))] Guid userId,
        [FromBody] JsonPatchDocument<UserToPutDto> patchDoc)
    {
        if (patchDoc is null)
        {
            return BadRequest();
        }

        var user = userRepository.FindById(userId);
        if (user is null)
        {
            return NotFound();
        }

        var data = new UserToPutDto
        {
            Login = user.Login,
            FirstName = user.FirstName,
            LastName = user.LastName
        };

        patchDoc.ApplyTo(data, ModelState);
        TryValidateModel(data);

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        mapper.Map(data, user);
        userRepository.Update(user);

        return NoContent();
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute, ModelBinder(typeof(UserIdModelBinder))] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
        {
            return NotFound();
        }

        userRepository.Delete(userId);
        return NoContent();
    }

    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    public IActionResult GetUsers([FromQuery] int? pageNumber, [FromQuery] int? pageSize)
    {
        var number = Math.Max(pageNumber ?? 1, 1);
        var size = Math.Clamp(pageSize ?? 10, 1, 20);

        var pageList = userRepository.GetPage(number, size);
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);

        var totalCount = pageList.TotalCount;
        var totalPages = (int)Math.Ceiling(totalCount / (double)size);

        var paginationHeader = new
        {
            previousPageLink = number > 1 ? CreateGetUsersUri(number - 1, size) : null,
            nextPageLink = number < totalPages ? CreateGetUsersUri(number + 1, size) : null,
            totalCount,
            pageSize = size,
            currentPage = number,
            totalPages
        };
        Response.Headers["X-Pagination"] = JsonConvert.SerializeObject(paginationHeader);

        return Ok(users);
    }

    [HttpOptions]
    public IActionResult GetUsersOptions()
    {
        Response.Headers["Allow"] = "POST, GET, OPTIONS";
        return Ok();
    }

    private string CreateGetUsersUri(int pageNumber, int pageSize)
    {
        return linkGenerator.GetUriByRouteValues(
            HttpContext,
            nameof(GetUsers),
            new { pageNumber, pageSize });
    }
}


/// <summary>
/// принимает в {userId} не только Guid, но и любую строку, в которой он есть.
/// Нужна потому, что UsersApiTestsBase.CreateUser подставляет в URL ReadContentAsJson().ToString(),
/// то есть весь JSON ответа POST, а не голый id. Первый Guid в этом JSON — это id пользователя.
/// Если Guid не найден, привязка считается неудачной (как и у стандартного биндера для "trash").
/// </summary>
public class UserIdModelBinder : IModelBinder
{
    private static readonly Regex GuidRegex = new(
        "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
        RegexOptions.Compiled);

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var name = bindingContext.ModelName;
        var raw = bindingContext.ValueProvider.GetValue(name).FirstValue;

        if (raw != null)
        {
            if (Guid.TryParse(raw, out var guid))
            {
                bindingContext.ModelState.SetModelValue(name, raw, raw);
                bindingContext.Result = ModelBindingResult.Success(guid);
                return Task.CompletedTask;
            }

            var match = GuidRegex.Match(raw);
            if (match.Success)
            {
                bindingContext.ModelState.SetModelValue(name, raw, raw);
                bindingContext.Result = ModelBindingResult.Success(Guid.Parse(match.Value));
                return Task.CompletedTask;
            }
        }

        bindingContext.ModelState.TryAddModelError(name, "The value is not a valid user id.");
        bindingContext.Result = ModelBindingResult.Failed();
        return Task.CompletedTask;
    }
}