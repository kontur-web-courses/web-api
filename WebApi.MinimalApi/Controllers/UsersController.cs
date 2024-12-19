using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

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
    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(404, "User not found")]
    [SwaggerResponse(200, "OK", typeof(UserDto))]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var innerUser = userRepository.FindById(userId);
        if (innerUser is not null)
        {
            var userDto = mapper.Map<UserDto>(innerUser);
            return Ok(userDto);
        }
        return NotFound();
    }

    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(400, "Data error")]
    [SwaggerResponse(422, "Validation error")]
    [SwaggerResponse(201, "Created")]
    public IActionResult CreateUser([FromBody] object user)
    {
        if (user != null)
        {
            if (ModelState.IsValid)
            {
                var userEntity = userRepository.Insert(mapper.Map<UserEntity>(user));
                return CreatedAtRoute(nameof(GetUserById), new { userId = userEntity.Id }, userEntity.Id);
            }

            return UnprocessableEntity(ModelState);
        }

        return BadRequest();
    }
    
    [HttpPut("{userId}")]
    [Consumes("application/json")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(400, "Data error")]
    [SwaggerResponse(422, "Validation error")]
    [SwaggerResponse(201, "User created")]
    [SwaggerResponse(204, "User updated")]
    public IActionResult UpdateUser([FromRoute] Guid? userId, [FromBody] UserDtoToUpdate? user)
    {
        if (user == null || ModelState.GetValidationState(nameof(iserId)) == ModelValidationState.Invalid)
            return BadRequest();
        if (ModelState.IsValid)
        {
            userId ??= Guid.NewGuid();
            var entity = mapper.Map(user, new UserEntity(userId.Value));
            userRepository.UpdateOrInsert(entity, out var inserted);
            return inserted
                ? CreatedAtRoute(nameof(GetUserById), new { userId = userId.Value }, entity.Id)
                : NoContent();
        }

        return UnprocessableEntity(ModelState);
    }
    
    [HttpPatch("{userId}")]
    [Consumes("application/json-patch+json")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(400, "Data error")]
    [SwaggerResponse(404, "User not found")]
    [SwaggerResponse(422, "Validation error")]
    [SwaggerResponse(204, "User updated")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserDtoToUpdate>? patch)
    {
        if (patch == null)
            return BadRequest();
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();
        if (ModelState.IsValid)
        {
            var tuUpd = mapper.Map<UserDtoToUpdate>(user);
            patch.ApplyTo(toUpdate, ModelState);
            if (TryValidateModel(tuUpd))
            {
                var entity = mapper.Map(tuUpd, user);
                userRepository.Update(entity);
                return NoContent();
            }

            return UnprocessableEntity(ModelState);
        }

        return UnprocessableEntity(ModelState);
    }
    
    [HttpDelete("{userId}")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(404, "User not found")]
    [SwaggerResponse(204, "User deleted succesfully")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        if (userRepository.FindById(userId) != null)
        {
            userRepository.Delete(userId);
            return NoContent();
        }

        return NotFound();
    }
    
    [HttpHead("{userId:guid}")]
    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    [SwaggerResponse(404, "User not found")]
    [SwaggerResponse(200, "OK", typeof(UserDto))]
    public IActionResult Head(Guid userId)
    {
        if (userRepository.FindById(userId) is not null)
        {
            HttpContext.Response.ContentType = "application/json; charset=utf-8";
            return Ok();
        }

        return NotFound();
    }
    
    [HttpGet(Name = nameof(GetUsers))]
    [Produces("application/json", "application/xml")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
    public ActionResult<IEnumerable<UserDto>> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1)
            pageNumber = 1;
        if (pageSize < 1)
            pageSize = 1;
        else if (pageSize > 20) pageSize = 20;
        var usersList = userRepository.GetPage(pageNumber, pageSize);
        var header = new
        {
            previousPageLink = usersList.CurrentPage <= 1
                ? null : linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new
                {
                    pageNumber = usersList.CurrentPage - 1,
                    pageSize = usersList.PageSize
                }),
            nextPageLink = usersList.CurrentPage >= 20
                ? null : linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new
                {
                    pageNumber = usersList.CurrentPage + 1,
                    pageSize = usersList.PageSize
                }),
            totalCount = usersList.TotalCount,
            pageSize = usersList.PageSize,
            currentPage = usersList.CurrentPage,
            totalPages = usersList.TotalPages
        };
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(header));
        return Ok(usersList);
    }

    [HttpOptions]
    [SwaggerResponse(200, "OK")]
    public IActionResult Options()
    {
        Response.Headers.AppendList(
            HeaderNames.Allow, new[] { HttpMethods.Post, HttpMethods.Get, HttpMethods.Options });
        return Ok();
    }
}