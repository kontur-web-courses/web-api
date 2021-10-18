using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        private IUserRepository userRepository;
        private IMapper mapper;

        private LinkGenerator linkGenerator;
        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
        {
            this.userRepository = userRepository;
            this.mapper = mapper;
            this.linkGenerator = linkGenerator;
        }

        [HttpHead("{userId}")]
        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId);
            if (user is null)
            {
                return NotFound();
            }

            var userDto = mapper.Map<UserDto>(user);
            return Ok(userDto);
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] UserCreationDto user)
        {
            if (user == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }
            // if (String.IsNullOrEmpty(user.Login) || user.Login.Any(c => !Char.IsLetterOrDigit(c)))
            if (user.Login.Any(c => !Char.IsLetterOrDigit(c)))
            {
                ModelState.AddModelError("login", "Некорректный логин");
                return UnprocessableEntity(ModelState);
            }
            
            
            var userEntity = mapper.Map<UserEntity>(user);
            var createdUserEntity = userRepository.Insert(userEntity);
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
        }

        [HttpPut("{userId}")]
        public ActionResult<UserEntity> UpdateUser([FromRoute] string userId, [FromBody] UserUpdateDto user)
        {
            if (!Guid.TryParse(userId, out Guid guid) || user is null)
            {
                return BadRequest();
            }
            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            UserEntity newUser = mapper.Map(user, new UserEntity(guid));
            
            
            userRepository.UpdateOrInsert(newUser, out bool inserted);

            if (inserted)
            {
                return CreatedAtRoute(
                    nameof(GetUserById),
                    new { userId = newUser.Id },
                    newUser.Id);
            }
            else
            {
                return NoContent();
            }

        }

        [HttpPatch("{userId}")]
        public IActionResult PatchUser([FromRoute] Guid userId,
            [FromBody] JsonPatchDocument<UserUpdateDto> PatchDoc)
        {
            if (userId.Equals(Guid.Empty))
            {
                return NotFound();
            }

            if (PatchDoc is null)
            {
                return BadRequest();
            }

            UserEntity user = userRepository.FindById(userId);
            if (user is null)
            {
                return NotFound();
            }

            UserUpdateDto updateDto = mapper.Map<UserUpdateDto>(user);
            PatchDoc.ApplyTo(updateDto, ModelState);
            UserEntity patchedUser = mapper.Map(updateDto, new UserEntity(userId));
            Regex regex = new Regex("^[0-9\\p{L}]*$");
            if (String.IsNullOrEmpty(patchedUser.Login) ||
                !regex.IsMatch(patchedUser.Login))
            {
                ModelState.AddModelError("login", "Login must be not empty and should contain only letters or digits");
            }

            if (String.IsNullOrEmpty(patchedUser.FirstName))
            {
                ModelState.AddModelError("firstName", "FirstName must not be empty");
            }
            
            if (String.IsNullOrEmpty(patchedUser.LastName))
            {
                ModelState.AddModelError("lastName", "LastName must not be empty");
            }
            
            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }
            
            userRepository.Update(patchedUser);

            return NoContent();
        }

        [HttpDelete("{userId}")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            if (userId.Equals(Guid.Empty) || userRepository.FindById(userId) is null)
            {
                return NotFound();
            }
            
            userRepository.Delete(userId);
            return NoContent();
        }

        [HttpGet(Name = nameof(GetUsers))]
        public IActionResult GetUsers(
            [FromQuery(Name = "pageNumber")] int pageNumber = 1,
            [FromQuery(Name = "pageSize")] int pageSize = 10)
        {
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Max(pageSize, 1);
            pageSize = Math.Min(pageSize, 20);
            
            var pageList = userRepository.GetPage(pageNumber, pageSize);
            var users = mapper.Map<IEnumerable<UserDto>>(pageList);
            string previousePageLinkIfExists = null;
            string nextPageLinkIfExists = null;
            if (pageList.HasPrevious)
            {
                previousePageLinkIfExists = linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new
                {
                    pageNumber = pageNumber - 1,
                    pageSize
                });
            }
            if (pageList.HasNext)
            {
                nextPageLinkIfExists = linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new
                {
                    pageNumber = pageNumber + 1,
                    pageSize
                });
            }
            var paginationHeader = new
            {
                previousPageLink = previousePageLinkIfExists,
                nextPageLink = nextPageLinkIfExists,
                totalCount = pageList.TotalCount,
                pageSize = pageList.PageSize,
                currentPage = pageList.CurrentPage,
                totalPages = pageList.TotalPages,
            };
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
            
            return Ok(users);
        }

        public IActionResult OptionsUsers()
        {
            string[] allowedMethods = new[]
            {
                "GET", "POST", "OPTIONS"
            };
            Response.Headers.Add("Allow", String.Join(", ", allowedMethods));

            return Ok();
        }
    }
}