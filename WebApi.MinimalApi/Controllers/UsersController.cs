using System.Text.RegularExpressions;
using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
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
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var userEntity = userRepository.FindById(userId);

        if (userEntity is null)
            return NotFound();

        var userDto = mapper.Map<UserDto>(userEntity);

        if (Request.Method == "HEAD")
        {
            Response.Body = Stream.Null;
        }
        
        return Ok(userDto);
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] CreateUserDto user)
    {
        if (user is null)
        {
            return BadRequest();
        }
        
        if (user.Login == "" || user.Login is null)
        {
            ModelState.AddModelError("login", "Error");
            return UnprocessableEntity(ModelState);
        }
        
        if (!user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("login", "Error");
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var userToCreate = mapper.Map<UserEntity>(user);
        var createdUserEntity = userRepository.Insert(userToCreate);

        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromBody] UpdateUserDto user, [FromRoute] string userId)
    {
        if (user is null)
        {
            return BadRequest();
        }
        
        var validationResult = ValidateUserDto(user);
        if (validationResult != null)
        {
            return validationResult;
        }

        var userToUpdate = mapper.Map<UserEntity>(user);

        if (!Guid.TryParse(userId, out var guid))
        {
            return BadRequest();
        }

        var userEntity = userRepository.FindById(guid);
        
        if (userEntity == null)
        {
            var createdUserEntity = userRepository.Insert(userToUpdate);
            
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
        }
        
        userRepository.Update(userToUpdate);

        return NoContent();
    }

    [HttpPatch("{userId}")]
    
    public IActionResult PartiallyUpdateUser([FromBody] JsonPatchDocument<UpdateUserDto> patchDoc, [FromRoute] String userId)
    {
        if (patchDoc == null)
        {
            return BadRequest();
        }
        
        var validationResult = ValidatePatchDocument(patchDoc);
        if (validationResult != null)
        {
            return validationResult;
        }

        if (!Guid.TryParse(userId, out var guid))
        {
            return NotFound();
        }
        
        var user = userRepository.FindById(guid);
        if (user == null)
        {
            return NotFound();
        }
        
        var updateUserDto = mapper.Map<UpdateUserDto>(user);
        patchDoc.ApplyTo(updateUserDto, ModelState);
        TryValidateModel(updateUserDto);

        return ModelState.IsValid ? NoContent() : UnprocessableEntity(ModelState);
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser(string userId)
    {
        if (!Guid.TryParse(userId, out var guid))
        {
            return NotFound();
        }

        if (userRepository.FindById(guid) == null)
        {
            return NotFound();
        }

        userRepository.Delete(guid);

        return NoContent();
    }

    [HttpGet]
    public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1) 
            pageNumber = 1;
        
        if (pageSize < 1) 
            pageSize = 1;
        
        if (pageSize > 20) 
            pageSize = 20;
        
        var pageList = userRepository.GetPage(pageNumber, pageSize);
        var users = mapper.Map<IEnumerable<UserDto>>(pageList);
        
        var paginationHeader = new
        {
            previousPageLink = pageList.HasPrevious ?
                linkGenerator.GetUriByAction(
                    HttpContext, nameof(GetUsers), 
                    values: new { pageNumber = pageNumber - 1, pageSize }) : null,
            nextPageLink = pageList.HasNext ?
                linkGenerator.GetUriByAction(
                    HttpContext, nameof(GetUsers), 
                    values: new { pageNumber = pageNumber + 1, pageSize }) : null,
            totalCount = pageList.TotalCount,
            pageSize = pageSize,
            currentPage = pageNumber,
            totalPages = (int)Math.Ceiling((double)pageList.TotalCount / pageSize)
        };
        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
        
        return Ok(users);
    }
    
    [HttpOptions]
    public IActionResult Options()
    {
        Response.Headers.Add("Allow", "GET, POST, OPTIONS");

        return Ok();
    }
    
    private IActionResult ValidateUserDto(UpdateUserDto userDto)
    {
        if (string.IsNullOrWhiteSpace(userDto.Login) || !userDto.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("login", "Login must contain only letters and digits and cannot be empty.");
            return UnprocessableEntity(ModelState);
        }

        if (string.IsNullOrWhiteSpace(userDto.FirstName))
        {
            ModelState.AddModelError("firstName", "First name cannot be empty.");
            return UnprocessableEntity(ModelState);
        }

        if (string.IsNullOrWhiteSpace(userDto.LastName))
        {
            ModelState.AddModelError("lastName", "Last name cannot be empty.");
            return UnprocessableEntity(ModelState);
        }
        
        return null;
    }
    
    private IActionResult ValidatePatchDocument(JsonPatchDocument<UpdateUserDto> patchDoc)
    {
        foreach (var operation in patchDoc.Operations)
        {
            if (operation.path == "login")
            {
                if (ContainsSpecialCharacters(operation.value.ToString()))
                {
                    ModelState.AddModelError("login", "Login must not contain special characters.");
                    return UnprocessableEntity(ModelState);
                }
                if (string.IsNullOrWhiteSpace(operation.value.ToString()))
                {
                    ModelState.AddModelError("login", "Login cannot be empty.");
                    return UnprocessableEntity(ModelState);
                }
            }
            else if (operation.path == "firstName" && string.IsNullOrWhiteSpace(operation.value.ToString()))
            {
                ModelState.AddModelError("firstName", "First name cannot be empty.");
                return UnprocessableEntity(ModelState);
            }
            else if (operation.path == "lastName" && string.IsNullOrWhiteSpace(operation.value.ToString()))
            {
                ModelState.AddModelError("lastName", "Last name cannot be empty.");
                return UnprocessableEntity(ModelState);
            }
        }
        return null;
    }
    
    static bool ContainsSpecialCharacters(string str)
    {
        // регулярное выражение для проверки на наличие специальных символов
        var pattern = @"[^a-zA-Z0-9а-яА-ЯёЁ]";

        // Проверяем, соответствует ли строка шаблону
        return Regex.IsMatch(str, pattern);
    }
}