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

        [HttpGet(Name = nameof(GetUsers))]
        [Produces("application/json", "application/xml")]
        public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 20);
            var pageList = userRepository.GetPage(pageNumber, pageSize);
            var paginationHeader = new
            {
                previousPageLink = pageList.HasPrevious ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers),
                    new { pageNumber = pageList.CurrentPage - 1, pageSize = pageList.PageSize }) : null,
                nextPageLink = pageList.HasNext ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers),
                    new { pageNumber = pageList.CurrentPage + 1, pageSize = pageList.PageSize }) : null,
                totalCount = pageList.TotalCount,
                pageSize = pageList.PageSize,
                currentPage = pageList.CurrentPage,
                totalPages = pageList.TotalPages
            };
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
            return Ok(mapper.Map<IEnumerable<UserDto>>(pageList));
        }

        [HttpHead("{userId}")]
        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [Produces("application/json", "application/xml")]
        public IActionResult GetUserById([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId);
            if (user is null)
                return NotFound();
            return Ok(mapper.Map<UserDto>(user));
        }

        [HttpPost]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] CreateUserDto createUserDto)
        {
            if (createUserDto is null)
                return BadRequest();

            if (createUserDto.Login is not null && !createUserDto.Login.All(char.IsLetterOrDigit))
                ModelState.AddModelError("Login", "Логин должен состоять только из символов и цифр");

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var user = userRepository.Insert(mapper.Map<UserEntity>(createUserDto));
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = user.Id },
                user.Id);
        }

        [HttpPut("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdateUserDto updateUserDto)
        {
            if (userId == Guid.Empty || updateUserDto is null)
                return BadRequest();

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var entityToUpdate = new UserEntity(userId);
            mapper.Map(updateUserDto, entityToUpdate);
            userRepository.UpdateOrInsert(entityToUpdate, out var isInserted);

            var entity = userRepository.FindById(userId);
            if (isInserted)
                return CreatedAtRoute(nameof(GetUserById), new { userId = entity.Id }, entity.Id);
            return NoContent();
        }

        [HttpPatch("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult PatchUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc)
        {
            if (patchDoc is null)
                return BadRequest();

            if (userRepository.FindById(userId) is null)
                return NotFound();

            var updateDto = new UpdateUserDto();
            patchDoc.ApplyTo(updateDto, ModelState);

            if (!TryValidateModel(updateDto))
                return UnprocessableEntity(ModelState);

            var entityToUpdate = new UserEntity(userId);
            mapper.Map(updateDto, entityToUpdate);

            userRepository.Update(entityToUpdate);
            return NoContent();
        }

        [HttpDelete("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            if (userRepository.FindById(userId) == null)
                return NotFound();

            userRepository.Delete(userId);
            return NoContent();
        }

        [HttpOptions]
        public IActionResult GetOptions()
        {
            Response.Headers.Add("Allow", "GET, POST, OPTIONS");
            return Ok();
        }
    }
}