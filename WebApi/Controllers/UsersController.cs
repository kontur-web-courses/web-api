using System;
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
    [Produces("application/json", "application/xml")]
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
            if (userId == Guid.Empty)
                return NotFound();
            
            var user = userRepository.FindById(userId);

            if (user is null)
                return NotFound();
            
            return Ok(mapper.Map<UserEntity, UserDto>(user));
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] CreateUserDto user)
        {
            if (user is null)
                return BadRequest();

            if (string.IsNullOrEmpty(user.Login) || user.Login.Any(c => !char.IsLetterOrDigit(c)))
                ModelState.AddModelError("Login", "Login should contain only letters or digits");

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            
            var createdUserEntity = userRepository.Insert(
                mapper.Map<CreateUserDto, UserEntity>(user)
                );
            
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
        }

        [HttpPut("{userId}")]
        public IActionResult UpdateUser([FromBody] UpdateUserDto user, [FromRoute] Guid userId)
        {
            if (user is null || userId == Guid.Empty)
                return BadRequest();
            
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var userEntity = mapper.Map(user, new UserEntity(userId));
            
            userRepository.UpdateOrInsert(userEntity, out var isInserted);

            if (!isInserted)
                return NoContent();

            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId },
                userId);
        }

        [HttpPatch("{userId}")]
        public IActionResult PartiallyUpdateUser([FromBody] JsonPatchDocument<UpdateUserDto> patchDoc, [FromRoute] Guid userId)
        {
            if (patchDoc is null)
                return BadRequest();
            
            var currentUser = userRepository.FindById(userId);
            
            if (currentUser is null)
                return NotFound();
            
            var updateUser = mapper.Map<UserEntity, UpdateUserDto>(currentUser);
            
            patchDoc.ApplyTo(updateUser, ModelState);

            if (!TryValidateModel(updateUser))
                return UnprocessableEntity(ModelState);
            
            userRepository.Update(
                mapper.Map<UpdateUserDto, UserEntity>(updateUser)
                );

            return NoContent();
        }

        [HttpDelete("{userId}")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            var deletedUser = userRepository.FindById(userId);

            if (deletedUser is null)
                return NotFound();
            
            userRepository.Delete(userId);

            return NoContent();
        }

        [HttpGet(Name = nameof(GetUsers))]
        public IActionResult GetUsers([FromQuery] int pageSize = 10, [FromQuery] int pageNumber = 1)
        {
            pageSize = Math.Max(Math.Min(20, pageSize), 1);
            pageNumber = Math.Max(1, pageNumber);

            var pageUsers = userRepository.GetPage(pageNumber, pageSize);
            
            var paginationHeader = new
            {
                previousPageLink = pageUsers.HasPrevious
                    ? GetUriUsers(pageNumber - 1, pageSize)
                    : null,
                nextPageLink = pageUsers.HasNext 
                    ? GetUriUsers(pageNumber + 1, pageSize)
                    : null,
                totalCount = pageUsers.TotalCount,
                pageSize = pageUsers.PageSize,
                currentPage = pageUsers.CurrentPage,
                totalPages = pageUsers.TotalPages
            };
            
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

            return Ok(paginationHeader);
        }

        private string GetUriUsers(int pageNumber, int pageSize)
        {
            return linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageSize, pageNumber });
        }
    }
}