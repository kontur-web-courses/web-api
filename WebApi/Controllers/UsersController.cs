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
    [Produces("application/json", "application/xml")]
    public class UsersController : Controller
    {
        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
        {
            this.userRepository = userRepository;
            this.mapper = mapper;
            this.linkGenerator = linkGenerator;
        }

        [HttpGet("{userId:guid}", Name = nameof(GetUserById))]
        [HttpHead("{userId}")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return NotFound();
            }

            var user = userRepository.FindById(userId);
            if (user is null)
            {
                return NotFound();
            }

            return Ok(mapper.Map<UserEntity, UserDto>(user));
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] CreateUserDto? userDto)
        {
            if (userDto is null)
            {
                return BadRequest();
            }

            if (string.IsNullOrEmpty(userDto.Login) || userDto.Login.Any(x => !char.IsLetterOrDigit(x)))
            {
                ModelState.AddModelError("Login", "ты писать н еумеешь");
            }

            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            var userEntity = mapper.Map<CreateUserDto, UserEntity>(userDto);
            var newUser = userRepository.Insert(userEntity);

            return CreatedAtRoute(nameof(GetUserById), new {userId = newUser.Id}, newUser.Id);
        }

        [HttpPut("{userId}")]
        public IActionResult UpdateUser([FromBody] UpdateUserDto? userDto, [FromRoute] Guid userId)
        {
            if (userDto is null || userId == Guid.Empty)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            var userEntity = mapper.Map<UpdateUserDto, UserEntity>(userDto);
            userEntity.Id = userId;

            userRepository.UpdateOrInsert(userEntity, out var isInserted);

            if (isInserted)
            {
                return CreatedAtRoute(nameof(GetUserById), new {userId = userId}, userId);
            }

            return NoContent();
        }


        [HttpPatch("{userId:guid}")]
        public IActionResult PartiallyUpdateUser([FromBody] JsonPatchDocument<UpdateUserDto>? patchDoc, [FromRoute] Guid userId)
        {
            if (patchDoc is null || userId == Guid.Empty)
            {
                return BadRequest();
            }

            var currentUser = userRepository.FindById(userId);
            if (currentUser is null)
            {
                return NotFound();
            }

            var updateUser = mapper.Map<UserEntity, UpdateUserDto>(currentUser);
            patchDoc.ApplyTo(updateUser, ModelState);

            if (!TryValidateModel(updateUser))
            {
                return UnprocessableEntity(ModelState);
            }

            var userEntity = mapper.Map<UpdateUserDto, UserEntity>(updateUser);

            userRepository.Update(userEntity);
            return NoContent();
        }

        [HttpDelete("{userId:guid}")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            if (userRepository.FindById(userId) is null)
                return NotFound();
            userRepository.Delete(userId);
            return NoContent();
        }

        [HttpGet]
        public IActionResult GetUsers([FromQuery] int pageSize = 10, [FromQuery] int pageNumber = 1)
        {
            var pageList = userRepository.GetPage(pageNumber, pageSize);
            var users = mapper.Map<IEnumerable<UserDto>>(pageList);

            var totalPages = userRepository.UsersCount % pageSize == 0
                ? userRepository.UsersCount / pageSize
                : userRepository.UsersCount / pageSize + 1;
            if (pageSize is > 20 or < 1 || pageNumber < 1 || pageNumber > totalPages)
                return BadRequest();


            var paginationHeader = CreatePaginationHeader(pageSize,
                pageNumber,
                totalPages);

            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
            return Ok(users);
        }

        private Dictionary<string, string?> CreatePaginationHeader(int pageSize, int pageNumber, int totalPages)
        {
            var paginationHeader = new Dictionary<string, string?>()
            {
                {"totalCount", userRepository.UsersCount.ToString()},
                {"pageSize", pageSize.ToString()},
                {"currentPage", pageNumber.ToString()},
                {"totalPages", totalPages.ToString()},
            };

            var nextPageNumber = pageNumber + 1;
            var previousPageNumber = pageNumber - 1;
            
            if (previousPageNumber >= 1)
            {
                var previousPageLink = linkGenerator.GetUriByRouteValues(HttpContext,
                    "users",
                    new {pageNumber = pageNumber - 1, pageSize});
                paginationHeader["previousPageLink"] = previousPageLink;
            }

            if (nextPageNumber <= totalPages)
            {
                var nextPageLink = linkGenerator.GetUriByRouteValues(HttpContext,
                    "users",
                    new {pageNumber = pageNumber + 1, pageSize});
                paginationHeader["nextPageLink"] = nextPageLink;
            }

            return paginationHeader;
        }

        [HttpOptions]
        public IActionResult GetAllowed()
        {
            Response.Headers.Add("Allowed", string.Join(',', "POST", "GET", "OPTIONS"));
            return Ok();
        }

        private readonly IUserRepository userRepository;
        private readonly IMapper mapper;
        private readonly LinkGenerator linkGenerator;
    }
}