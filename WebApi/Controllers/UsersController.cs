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
        private readonly LinkGenerator linkGenerator;

        private readonly IMapper mapper;

        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        private readonly IUserRepository userRepository;

        public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
        {
            this.userRepository = userRepository;
            this.mapper = mapper;
            this.linkGenerator = linkGenerator;
        }

        [HttpHead("{userId}", Name = nameof(GetUserById))]
        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var userFromRepository = userRepository.FindById(userId);
            if (userFromRepository == null)
                return NotFound();

            var userDto = mapper.Map<UserDto>(userFromRepository);
            return Ok(userDto);
        }

        [HttpPost]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] UserToCreate user)
        {
            if (user == null)
                return BadRequest();

            if (string.IsNullOrEmpty(user.Login) || !user.Login.All(char.IsLetterOrDigit))
                ModelState.AddModelError(nameof(UserToCreate.Login), "Логин должен содержать только буквы или цифры");

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var userEntity = mapper.Map<UserEntity>(user);
            var createdUserEntity = userRepository.Insert(userEntity);

            return CreatedAtRoute(
                nameof(GetUserById),
                new {userId = createdUserEntity.Id},
                createdUserEntity.Id);
        }

        [HttpPut("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UserToUpdate user)
        {
            if (user == null || userId == Guid.Empty)
                return BadRequest();

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var userEntity = new UserEntity(userId);
            mapper.Map(user, userEntity);
            userRepository.UpdateOrInsert(userEntity, out var isInserted);

            if (isInserted)
                return CreatedAtRoute(
                    nameof(GetUserById),
                    new {userId = userEntity.Id},
                    userEntity.Id);

            return NoContent();
        }

        [HttpPatch("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserToUpdate> patchDoc)
        {
            if (patchDoc == null)
                return BadRequest();

            var userFromRepository = userRepository.FindById(userId);
            if (userFromRepository == null)
                return NotFound();

            var user = mapper.Map<UserToUpdate>(userFromRepository);

            patchDoc.ApplyTo(user, ModelState);
            TryValidateModel(user);

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            mapper.Map(user, userFromRepository);
            userRepository.Update(userFromRepository);

            return NoContent();
        }


        [HttpDelete("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            var userFromRepository = userRepository.FindById(userId);
            if (userFromRepository == null)
                return NotFound();

            userRepository.Delete(userId);
            return NoContent();
        }

        [HttpGet(Name = nameof(GetUsers))]
        [Produces("application/json", "application/xml")]
        public ActionResult<IEnumerable<UserDto>> GetUsers([FromQuery] int pageNumber=1, [FromQuery] int pageSize=10)
        {
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Min(Math.Max(pageSize, 1), 20);

            var pageList = userRepository.GetPage(pageNumber, pageSize);

            var paginationHeader = new
            {
                previousPageLink = pageList.HasPrevious ? CreateGetUsersUri(pageList.CurrentPage - 1, pageList.PageSize) : null,
                nextPageLink = pageList.HasNext ? CreateGetUsersUri(pageList.CurrentPage + 1, pageList.PageSize) : null,
                totalCount = pageList.TotalCount,
                pageSize = pageList.PageSize,
                currentPage = pageList.CurrentPage,
                totalPages = pageList.TotalPages
            };
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

            return Ok(pageList);
        }
        
        [HttpOptions(Name = nameof(GetOptions))]
        [Produces("application/json", "application/xml")]
        public ActionResult GetOptions()
        {
            Response.Headers.Add("Allow", "GET, POST, OPTIONS");
            return Ok();
        }

        private string CreateGetUsersUri(int pageNumber, int pageSize)
        {
            return linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers),
                new
                {
                    pageNumber,
                    pageSize
                });
        }
    }
}