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
            this.mapper = mapper;
            this.userRepository = userRepository;
            this.linkGenerator = linkGenerator;
        }

        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [HttpHead("{userId}")]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var userEntity = userRepository.FindById(userId);
            return userEntity is null ? NotFound() : Ok(mapper.Map<UserDto>(userEntity));
        }

        [HttpPost]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] CreateDto userCreateDto)
        {
            if (userCreateDto is null) return BadRequest();
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);
            if (!userCreateDto.Login.All(char.IsLetterOrDigit))
            {
                ModelState.AddModelError("Login", "Login should contain only letters or digits");
                return UnprocessableEntity(ModelState);
            }

            var userEntity = mapper.Map<UserEntity>(userCreateDto);
            userEntity = userRepository.Insert(userEntity);
            return CreatedAtRoute(
                nameof(GetUserById),
                new {userId = userEntity.Id},
                userEntity.Id);
        }

        [HttpPut("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdateDto userUpdateDto)
        {
            if (userUpdateDto is null || userId == Guid.Empty)
                return BadRequest();
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

            var userEntity = new UserEntity(userId);
            mapper.Map(userUpdateDto, userEntity);
            userRepository.UpdateOrInsert(userEntity, out var isInserted);
            return !isInserted
                ? NoContent()
                : CreatedAtRoute(
                    nameof(GetUserById),
                    new {userId = userEntity.Id},
                    userEntity.Id);
        }

        [HttpPatch("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateDto> patchDoc)
        {
            if (patchDoc is null) return BadRequest();

            var userEntity = userRepository.FindById(userId);
            if (userEntity is null) return NotFound();

            var updateDto = mapper.Map<UpdateDto>(userEntity);
            patchDoc.ApplyTo(updateDto, ModelState);
            if (!TryValidateModel(updateDto)) return UnprocessableEntity(ModelState);
            userRepository.Update(mapper.Map<UserEntity>(updateDto));
            return NoContent();
        }

        [HttpDelete("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            if (userRepository.FindById(userId) is null)
                return NotFound();
            userRepository.Delete(userId);
            return NoContent();
        }

        [HttpGet(Name = nameof(GetUsers))]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUsers([FromQuery] int? pageNumber, [FromQuery] int? pageSize)
        {
            pageNumber ??= 1;
            if (pageNumber < 1) pageNumber = 1;

            pageSize = (int) (pageSize switch
            {
                null => 10,
                < 1 => 1,
                > 20 => 20,
                _ => pageSize
            });

            var pageList = userRepository.GetPage(pageNumber.Value, pageSize.Value);
            string previousPageLink = null;
            string nextPageLink = null;
            var previousPageNumber = pageNumber - 1;
            var nextPageNumber = pageNumber + 1;
            if (pageList.HasPrevious)
                previousPageLink = linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new {pageSize, previousPageNumber});
            if (pageList.HasNext)
                nextPageLink = linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new {pageSize, nextPageNumber});

            var paginationHeader = new
            {
                previousPageLink,
                nextPageLink,
                totalCount = pageList.TotalCount,
                pageSize,
                currentPage = pageNumber,
                totalPages = pageList.TotalPages
            };
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
            return Ok(paginationHeader);
        }
    }
}