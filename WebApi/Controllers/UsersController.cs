using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly LinkGenerator _linkGenerator;

        public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _linkGenerator = linkGenerator;
        }

        [HttpGet("{userId:guid}", Name = nameof(GetUserById))]
        [HttpHead("{userId}")]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById(Guid userId)
        {
            var user = _userRepository.FindById(userId);
            if (user == null) return NotFound();

            var userDto = _mapper.Map<UserDto>(user);
            return Ok(userDto);
        }

        [HttpPost]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser(CreateUserDto user)
        {
            if (user == null || string.IsNullOrEmpty(user.Login) || !user.Login.All(char.IsLetterOrDigit))
            {
                ModelState.AddModelError(nameof(CreateUserDto.Login), "Login should contain only letters or digits");
                return UnprocessableEntity(ModelState);
            }

            var userEntity = _mapper.Map<UserEntity>(user);
            var createdUser = _userRepository.Insert(userEntity);

            return CreatedAtRoute(nameof(GetUserById), new { userId = createdUser.Id }, createdUser.Id);
        }

        [HttpPut("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser(Guid userId, UpdateUserDto user)
        {
            if (user == null || userId == Guid.Empty) return BadRequest();

            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

            var userEntity = new UserEntity(userId);
            _mapper.Map(user, userEntity);

            _userRepository.UpdateOrInsert(userEntity, out var isInserted);

            return isInserted 
                ? CreatedAtRoute(nameof(GetUserById), new { userId = userEntity.Id }, userEntity.Id) 
                : NoContent();
        }

        [HttpPatch("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult PartiallyUpdateUser(Guid userId, JsonPatchDocument<UpdateUserDto> patchDoc)
        {
            if (patchDoc == null) return BadRequest();

            var userEntity = _userRepository.FindById(userId);
            if (userEntity == null) return NotFound();

            var updateDto = _mapper.Map<UpdateUserDto>(userEntity);
            patchDoc.ApplyTo(updateDto, ModelState);

            if (!TryValidateModel(updateDto) || !ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            _mapper.Map(updateDto, userEntity);
            _userRepository.Update(userEntity);

            return NoContent();
        }

        [HttpDelete("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult DeleteUser(Guid userId)
        {
            var user = _userRepository.FindById(userId);
            if (user == null) return NotFound();

            _userRepository.Delete(userId);
            return NoContent();
        }

        [HttpGet(Name = nameof(GetUsers))]
        [Produces("application/json", "application/xml")]
        public ActionResult<IEnumerable<UserDto>> GetUsers(int pageNumber = 1, int pageSize = 10)
        {
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Clamp(pageSize, 1, 20);
            var page = _userRepository.GetPage(pageNumber, pageSize);

            var paginationHeader = new
            {
                previousPageLink = page.HasPrevious ? CreateGetUsersUri(page.CurrentPage - 1, page.PageSize) : null,
                nextPageLink = page.HasNext ? CreateGetUsersUri(page.CurrentPage + 1, page.PageSize) : null,
                totalCount = page.TotalCount,
                pageSize = page.PageSize,
                currentPage = page.CurrentPage,
                totalPages = page.TotalPages
            };
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

            return Ok(page);
        }

        [HttpOptions]
        public IActionResult GetUsersOptions()
        {
            Response.Headers.Add("Allow", "POST,GET,OPTIONS");
            return Ok();
        }

        private string CreateGetUsersUri(int pageNumber, int pageSize) =>
            _linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber, pageSize });
    }
}