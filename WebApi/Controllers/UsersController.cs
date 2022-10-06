using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        private readonly IUserRepository userRepository;
        private readonly IMapper mapper;
        private readonly LinkGenerator linkGenerator;

        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
        {
            this.userRepository = userRepository;
            this.mapper = mapper;
            this.linkGenerator = linkGenerator;
        }

        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [HttpHead("{userId}")]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var userEntity = userRepository.FindById(userId);
            return userEntity == null
              ? NotFound() 
                : Ok(mapper.Map<UserDto>(userEntity));
        }

        [HttpPost(Name = nameof(CreateUser))]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] UserCreationDto user)
        {
            if (user == null)
                return BadRequest();
            
            if (string.IsNullOrEmpty(user.Login))
            {
                ModelState.AddModelError("Login", "Gde login, Lebovski???");
                return UnprocessableEntity(ModelState);
            }
                
            
            if (!user.Login.All(char.IsLetterOrDigit))
            {
                ModelState.AddModelError("Login", "Login must consist of letters and digits only!!! tupie");
                return UnprocessableEntity(ModelState);
            }
            var userMapped = mapper.Map<UserEntity>(user);
            var userEntity = userRepository.Insert(userMapped);
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id);
        }

        [HttpPut("{userId}", Name = nameof(UpdateUser))]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser([FromBody] UserUpdateDto user, [FromRoute] Guid userId)
        {
            if (user == null || userId == Guid.Empty)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            user.Id = userId;
            var userEntity = mapper.Map<UserEntity>(user);
            userRepository.UpdateOrInsert(userEntity, out var isInserted);
            return isInserted
                ? CreatedAtRoute(
                    nameof(GetUserById),
                    new { userId = userEntity.Id },
                    userEntity.Id)
                : NoContent();
        }

        [HttpPatch("{userId}", Name = nameof(PartiallyUpdateUser))]
        [Produces("application/json", "application/xml")]
        public IActionResult PartiallyUpdateUser([FromBody] JsonPatchDocument<UserUpdateDto> patchDoc, [FromRoute] Guid userId)
        {
            if (userId == Guid.Empty)
                return NotFound();
            
            if (patchDoc == null)
                return BadRequest();
            
            var updates = new UserUpdateDto();
            patchDoc.ApplyTo(updates, ModelState);
            var userEntity = userRepository.FindById(userId);
            
            if (userEntity == null)
                return NotFound();

            if (updates.Login != null)
                userEntity.Login = updates.Login;
            if (updates.FirstName != null)
                userEntity.FirstName = updates.FirstName;
            if (updates.LastName != null)
                userEntity.LastName = updates.LastName;

            var errors = GetErrors(updates);
            foreach (var error in errors)
            {
                ModelState.AddModelError(error.key, error.value);
            }

            if (errors.Any())
            {
                return UnprocessableEntity(ModelState);
            }
            
            userRepository.Update(userEntity);
            return NoContent();
        }

        [HttpDelete("{userId}", Name = nameof(DeleteUser))]
        [Produces("application/json", "application/xml")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            var userEntity = userRepository.FindById(userId);
            if (userEntity == null)
            {
                return NotFound();
            }
            userRepository.Delete(userId);
            return NoContent();
        }

        [HttpGet(Name = nameof(GetUsers))]
        [Produces("application/json", "application/xml")]
        public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            
            pageSize = pageSize < 1 ? 1 :
                pageSize > 20 ? 20 :
                pageSize;

            var pageList = userRepository.GetPage(pageNumber, pageSize);
            var users = mapper.Map<IEnumerable<UserDto>>(pageList);

            var previousPageLink = !pageList.HasPrevious ?
                null :
                linkGenerator.GetUriByRouteValues(
                HttpContext, 
                nameof(GetUsers), 
                new
                {
                    pageNumber = pageNumber - 1, pageSize
                });

            var nextPageLink = !pageList.HasNext ?
                null :
                linkGenerator.GetUriByRouteValues(
                HttpContext,
                nameof(GetUsers),
                new
                {
                    pageNumber = pageNumber + 1, pageSize
                }
            );
            
            var paginationHeader = new
            {
                previousPageLink,
                nextPageLink,
                totalCount = pageList.TotalCount,
                pageSize = pageList.PageSize,
                currentPage = pageList.CurrentPage,
                totalPages = pageList.TotalPages,
            };
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
            return Ok(users);
        }
        
        [HttpOptions(Name = nameof(GetUserOptions))]
        public IActionResult GetUserOptions()
        {
            var allowedMethods = new[] { "GET", "POST", "OPTIONS" };
            Response.Headers.Add("Allow", allowedMethods);
            return Ok();
        }

        private IReadOnlyCollection<(string key, string value)> GetErrors(UserUpdateDto userUpdateDto)
        {
            var errors = new List<(string, string)>();
            if (IsInvalidString(userUpdateDto.Login))
            {
                errors.Add(("login", "ah tiz nehoroshiy, login zabil"));
            }
            if (IsInvalidString(userUpdateDto.FirstName))
            {
                errors.Add(("firstName", "ah tiz nehoroshiy, firstName zabil"));
            }
            if (IsInvalidString(userUpdateDto.LastName))
            {
                errors.Add(("lastName", "ah tiz nehoroshiy, lastName zabil"));
            }

            return errors;
        }
        private void SearchForErrors(UserUpdateDto upates)
        {
            
        }

        private bool IsInvalidString(string str)
        {
            return str != null
                   && (str == string.Empty || !str.All(char.IsLetterOrDigit));
        }
    }
}