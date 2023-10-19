using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using NUnit.Framework.Constraints;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly LinkGenerator _linkGenerator;
        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _linkGenerator = linkGenerator;
        }

        [Produces("application/json", "application/xml")]
        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [HttpHead("{userId}")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = _userRepository.FindById(userId);
            return (user is null) ? NotFound() : Ok(_mapper.Map<UserDto>(user));
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] UserCreateDto user)
        {
            if (user is null)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            var createdUser = _userRepository.Insert(_mapper.Map<UserEntity>(user));
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUser.Id },
                createdUser.Id);
        }

        [Produces("application/json", "application/xml")]
        [HttpPut("{userId}")]
        public IActionResult UpdateUser([FromBody] UserPutDto user, [FromRoute] Guid userId)
        {
            if (user is null || userId == Guid.Empty)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            var newUserEntity = new UserEntity(userId);
            _mapper.Map(user, newUserEntity);
            _userRepository.UpdateOrInsert(newUserEntity, out bool isInserted);

            if (!isInserted)
                return NoContent();

            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = newUserEntity.Id },
                newUserEntity.Id);
        }

        [Produces("application/json", "application/xml")]
        [HttpPatch("{userId}")]
        public IActionResult PartiallyUpdateUser([FromBody] JsonPatchDocument<UserPutDto> patchDoc, [FromRoute] Guid userId)
        {
            if (patchDoc is null)
            {
                return BadRequest();
            }
            var userEntity = _userRepository.FindById(userId);
            if (userEntity is null)
            {
                return NotFound();
            }


            var updateDto = _mapper.Map<UserPutDto>(userEntity);

            patchDoc.ApplyTo(updateDto, ModelState);

            if (updateDto is null || userId == Guid.Empty)
            {
                return NotFound();
            }
            if (!TryValidateModel(updateDto))
            {
                return UnprocessableEntity(ModelState);
            }

            return NoContent();
        }

        [Produces("application/json", "application/xml")]
        [HttpDelete("{userId}")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            var userEntity = _userRepository.FindById(userId);

            if (userEntity is null)
            {
                return NotFound();
            }

            _userRepository.Delete(userId);
            return NoContent();
        }

        [HttpGet()]
        [Produces("application/json", "application/xml")]
        public ActionResult<IEnumerable<UserDto>> GetUsers(
            [FromQuery] [Range(1, int.MaxValue)] [DefaultValue(1)]
            int pageNumber,
            [FromQuery] [Range(1, 20)] [DefaultValue(10)]
            int pageSize)
        {
            if (pageNumber < 1)
                pageNumber = 1;
            if (pageSize < 1)
                pageSize = 1;
            if (pageSize > 20)
                pageSize = 20;

            var pageList = _userRepository.GetPage(pageNumber, pageSize);
            var users = _mapper.Map<IEnumerable<UserDto>>(pageList);
            var pages = _userRepository.GetPage(pageNumber, pageSize);


            var paginationHeader = new
            {
                previousPageLink = pageNumber == 1 ? null : _linkGenerator.GetUriByPage(HttpContext,
                _linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers),
                new { pageNumber = pageNumber - 1, pageSize = pageSize })),
                nextPageLink = _linkGenerator.GetUriByPage(HttpContext,
                _linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers),
                new { pageNumber = pageNumber + 1, pageSize = pageSize })),
                totalCount = pageList.TotalCount,
                pageSize = pageSize,
                currentPage = pageNumber,
                totalPages = pageList.TotalPages,
            };

            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
            return Ok(users);

        }

        [HttpOptions()]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUsersOptions()
        {
            Response.Headers.Add("Allow", " GET, POST, OPTIONS");

            return Ok();
        }
    }
}