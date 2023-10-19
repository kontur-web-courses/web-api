using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AutoMapper;
using Game.Domain;
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
        private readonly IUserRepository userRepository;
        private readonly IMapper mapper;
        private readonly LinkGenerator linkGenerator;

        public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
        {
            this.userRepository = userRepository;
            this.mapper = mapper;
            this.linkGenerator = linkGenerator;
        }

        [HttpGet(Name = nameof(GetUsers))]
        [Produces("application/json", "application/xml")]
        public ActionResult<IEnumerable<UserDto>> GetUsers(
            [FromQuery] [Range(1, int.MaxValue)] [DefaultValue(1)]
            int pageNumber,
            [FromQuery] [Range(1, 20)] [DefaultValue(10)]
            int pageSize)
        {
            if (ModelState.GetFieldValidationState("pageNumber") == ModelValidationState.Invalid)
            {
                pageNumber = 1;
            }

            if (ModelState.GetFieldValidationState("pageSize") == ModelValidationState.Invalid)
            {
                if (pageSize < 1)
                    pageSize = 1;
                else if (pageSize > 20)
                    pageSize = 20;
            }


            var pageList = userRepository.GetPage(pageNumber, pageSize);
            var users = mapper.Map<IEnumerable<UserDto>>(pageList);

            var paginationHeader = new
            {
                previousPageLink = pageList.CurrentPage == 1
                    ? null
                    : linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new {pageNumber = pageNumber - 1, pageSize}),
                nextPageLink = pageList.CurrentPage == pageList.TotalPages
                    ? null
                    : linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new {pageNumber = pageNumber + 1, pageSize}),
                totalCount = pageList.TotalCount,
                pageSize = pageList.PageSize,
                currentPage = pageList.CurrentPage,
                totalPages = pageList.TotalPages,
            };


            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));


            return Ok(users);
        }

        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [HttpHead("{userId}")]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId);
            if (user == null)
            {
                return NotFound();
            }

            var userDto = mapper.Map<UserDto>(user);

            return Ok(userDto);
        }

        [HttpPost]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] UserToCreateDto user)
        {
            if (user == null)
            {
                return BadRequest();
            }

            if (user.Login != null && !user.Login.All(char.IsLetterOrDigit))
            {
                ModelState.AddModelError("Login", "Логин состоит не только из букв и цифр");
            }

            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            var createdUserEntity = userRepository.Insert(mapper.Map<UserEntity>(user));

            return CreatedAtRoute(
                nameof(GetUserById),
                new {userId = createdUserEntity.Id},
                createdUserEntity.Id);
        }

        [HttpPut("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser([FromRoute] Guid userId, UpdateUserDto user)
        {
            if (user == null || userId == Guid.Empty)
                return BadRequest();

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var userEntity = mapper.Map(user, new UserEntity(userId));

            bool isInsert;
            userRepository.UpdateOrInsert(userEntity, out isInsert);

            if (isInsert)
                return CreatedAtRoute(
                    nameof(GetUserById),
                    new {userId = userId},
                    userId);
            return NoContent();
        }

        [HttpDelete("{userId}")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId);
            if (user == null)
                return NotFound();
            userRepository.Delete(userId);

            return NoContent();
        }
    }
}