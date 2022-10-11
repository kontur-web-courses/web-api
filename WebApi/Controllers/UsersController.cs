using System;
using Game.Domain;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using AutoMapper;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.JsonPatch;
    using Microsoft.AspNetCore.Routing;
    using Newtonsoft.Json;

    [Route("api/users")]
    [ApiController]
    [Produces("application/json", "application/xml")]
    public class UsersController : Controller
    {
        private readonly IUserRepository userRepository;
        private readonly IMapper mapper;
        private readonly LinkGenerator linkGenerator;

        public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
        {
            this.linkGenerator = linkGenerator;
            this.userRepository = userRepository;
            this.mapper = mapper;
        }

        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [HttpHead("{userId}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId);

            if (user == null)
                return NotFound();

            return Ok(mapper.Map<UserDto>(user));
        }

        [HttpGet(Name = "GetUsers")]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<UserDto>> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Max(Math.Min(pageSize, 20), 1);

            var users = userRepository.GetPage(pageNumber, pageSize);

            var paginationHeader = new
            {
                previousPageLink = users.HasPrevious
                    ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new
                    {
                        pageNumber = pageNumber - 1,
                        pageSize
                    })
                    : null,
                nextPageLink = users.HasNext
                    ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new
                    {
                        pageNumber = pageNumber + 1,
                        pageSize
                    })
                    : null,
                totalCount = users.TotalCount,
                pageSize,
                currentPage = pageNumber,
                totalPages = users.TotalPages,
            };
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

            return Ok(mapper.Map<IEnumerable<UserDto>>(users));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]

        public IActionResult CreateUser([FromBody] CreateUserDto createUserDto)
        {
            if (createUserDto == null)
            {
                return BadRequest();
            }

            if (string.IsNullOrEmpty(createUserDto.Login) || !createUserDto.Login.All(char.IsLetterOrDigit))
                ModelState.AddModelError("Login", "Login");

            if (!ModelState.IsValid || createUserDto.Login == null)
            {
                return UnprocessableEntity(ModelState);
            }

            var userEntity = mapper.Map<UserEntity>(createUserDto);
            var createdUserEntity = userRepository.Insert(userEntity);

            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
        }

        [HttpPut("{userId}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] PutUserDto user)
        {
            if (user == null || userId == Guid.Empty)
            {
                return BadRequest();
            }

            var entity = mapper.Map<UserEntity>(user, options => options.Items["Id"] = userId);
            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            userRepository.UpdateOrInsert(entity, out var wasCreated);
            if (!wasCreated)
            {
                return NoContent();
            }

            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId },
                entity.Id);
        }

        [HttpPatch("{userId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public ActionResult Patch([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateDto> patchDoc)
        {
            if (patchDoc == null)
            {
                return BadRequest();
            }

            var userEntity = userRepository.FindById(userId);

            if (userEntity == null)
            {
                return NotFound();
            }

            var updateDto = mapper.Map<UpdateDto>(userEntity);
            patchDoc.ApplyTo(updateDto, ModelState);

            if (!TryValidateModel(updateDto))
            {
                return UnprocessableEntity(ModelState);
            }

            var updatedUserEntity = mapper.Map<UserEntity>(updateDto, opt => opt.Items["Id"] = userId);
            userRepository.Update(updatedUserEntity);

            return NoContent();
        }

        [HttpDelete("{userId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult Delete([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId);

            if (user == null)
            {
                return NotFound();
            }

            userRepository.Delete(userId);

            return NoContent();
        }

        [HttpOptions]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult Options()
        {
            Response.Headers.Add("Allow", "GET, POST, OPTIONS");
            return Ok();
        }
    }
}