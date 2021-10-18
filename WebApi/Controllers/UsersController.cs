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
        private readonly IUserRepository repository;
        private readonly IMapper mapper;
        private readonly LinkGenerator linkGenerator;

        public UsersController(IUserRepository repository, IMapper mapper, LinkGenerator linkGenerator)
        {
            this.repository = repository;
            this.mapper = mapper;
            this.linkGenerator = linkGenerator;
        }

        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [HttpHead("{userId}")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = repository.FindById(userId);

            if (user == null)
                return NotFound();

            return Ok(mapper.Map<UserDto>(user));
        }

        [HttpGet(Name = nameof(GetUsers))]
        public ActionResult<ICollection<UserDto>> GetUsers(int pageNumber = 1, int pageSize = 10)
        {
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Min(Math.Max(pageSize, 1), 20);

            var page = repository.GetPage(pageNumber, pageSize);

            var paginationHeader = new
            {
                previousPageLink = page.HasPrevious ? GetUri(page.CurrentPage - 1, page.PageSize) : null,
                nextPageLink = page.HasNext ? GetUri(page.CurrentPage + 1, page.PageSize) : null,
                totalCount = page.TotalCount,
                pageSize = page.PageSize,
                currentPage = page.CurrentPage,
                totalPages = page.TotalPages,
            };
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

            return Ok(page);
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] UserToCreateDto userToCreate)
        {
            if (userToCreate is null)
            {
                return BadRequest();
            }

            if ((string.IsNullOrEmpty(userToCreate.Login)
                || !userToCreate.Login.All(c => char.IsLetterOrDigit(c))))
            {
                ModelState.AddModelError(
                    nameof(userToCreate.Login),
                    "Login should contain only letters or digits");
            }

            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            var user = mapper.Map<UserEntity>(userToCreate);

            var createdUserEntity = repository.Insert(user);

            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
        }

        [HttpPut("{userId}")]
        public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UserToUpdateDto userToUpdate)
        {
            if (userToUpdate is null || userId == Guid.Empty)
                return BadRequest();

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var user = new UserEntity(userId);

            mapper.Map(userToUpdate, user);

            repository.UpdateOrInsert(user, out var isInserted);

            if (!isInserted)
                return NoContent();

            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = user.Id },
                user.Id);
        }

        [HttpPatch("{userId}")]
        public IActionResult PartiallyUpdateUser(
            [FromRoute] Guid userId,
            [FromBody] JsonPatchDocument<UserToUpdateDto> patchDoc)
        {
            if (patchDoc is null)
            {
                return BadRequest();
            }

            var user = repository.FindById(userId);
            if (user is null)
            {
                return NotFound();
            }

            var updateDto = new UserToUpdateDto();
            patchDoc.ApplyTo(updateDto, ModelState);

            if (!TryValidateModel(updateDto))
            {
                return UnprocessableEntity(ModelState);
            }

            mapper.Map(updateDto, user);
            repository.Update(user);

            return NoContent();
        }

        [HttpDelete("{userId}")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            var user = repository.FindById(userId);
            if (user is null)
            {
                return NotFound();
            }

            repository.Delete(userId);

            return NoContent();
        }
        
        [HttpOptions]
        public IActionResult GetUsersOptions()
        {
            Response.Headers.Add("Allow", "GET, POST, OPTIONS");
            
            return Ok();
        }

        private string GetUri(int pageNumber, int pageSize)
            => linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber, pageSize });
    }
}