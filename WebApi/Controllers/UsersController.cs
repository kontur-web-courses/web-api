using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using WebApi.Models;

namespace WebApi.Controllers
{
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
        
        [HttpOptions]
        public IActionResult GetOptions()
        {
            Response.Headers.Add("Allow","POST, GET, OPTIONS");
            return Ok();
        }

        [HttpGet(Name = nameof(GetUsers))]
        [Produces("application/json", "application/xml")]
        public ActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 20);
            var pageList = userRepository.GetPage(pageNumber, pageSize);
            var paginationHeader = new
            {
                previousPageLink = pageList.HasPrevious
                    ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new {pageNumber = pageNumber - 1, pageSize})
                    : null,
                nextPageLink = pageList.HasNext
                    ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new {pageNumber = pageNumber + 1, pageSize})
                    : null,
                totalCount = pageList.TotalCount,
                pageSize = pageList.PageSize,
                currentPage = pageList.CurrentPage,
                totalPages = pageList.TotalPages
            };
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
            var users = mapper.Map<IEnumerable<UserDto>>(pageList);
            return Ok(users);
        }

        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [HttpHead("{userId}")]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId);
            if (user is null)
            {
                return NotFound();
            }

            var userDTO = mapper.Map<UserDto>(user);
            return Ok(userDTO);
        }

        [HttpPut("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser([FromRoute] Guid userId, PutUserDto user)
        {
            if (user is null || userId == Guid.Empty)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            var userEntity = new UserEntity(id: userId);
            userEntity = mapper.Map(user, userEntity);
            bool isInserted;
            userRepository.UpdateOrInsert(userEntity, isInserted: out isInserted);
            if (isInserted)
            {
                return CreatedAtRoute(
                    nameof(GetUserById),
                    new {userId = userEntity.Id},
                    userEntity.Id);
            }

            return NoContent();
        }

        [HttpPatch("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult PartiallyUpdateUser([FromBody] JsonPatchDocument<PatchUserDto> patchDoc, Guid userId)
        {
            if (patchDoc is null)
            {
                return BadRequest();
            }

            if (userId == Guid.Empty)
            {
                return NotFound();
            }

            var user = userRepository.FindById(userId);
            if (user is null)
            {
                return NotFound();
            }

            var patchUserDto = new PatchUserDto();

            mapper.Map(user, patchUserDto);
            patchDoc.ApplyTo(patchUserDto, ModelState);
            TryValidateModel(patchUserDto);
            return !ModelState.IsValid ? UnprocessableEntity(ModelState) : NoContent();
        }

        [HttpDelete("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult DeleteUser(Guid userId)
        {
            var user = userRepository.FindById(userId);
            if (user is null)
            {
                return NotFound();
            }

            userRepository.Delete(userId);
            return NoContent();
        }

        [HttpPost]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] CreateUserDto user)
        {
            if (user is null)
            {
                return BadRequest();
            }

            if (user.Login is null)
            {
                ModelState.AddModelError("login", "Login should not be null");
            }


            else if (!user.Login.All(char.IsLetterOrDigit))
            {
                ModelState.AddModelError("login", "Login must have only letters and digits");
            }

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var userEntity = mapper.Map<UserEntity>(user);
            var createdUserEntity = userRepository.Insert(userEntity);
            return CreatedAtRoute(
                nameof(GetUserById),
                new {userId = createdUserEntity.Id},
                createdUserEntity.Id);
        }
    }
}