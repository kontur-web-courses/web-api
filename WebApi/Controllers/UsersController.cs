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
        // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
        private IUserRepository _userRepository;
        private IMapper _mapper;
        private LinkGenerator _linkGenerator;
        public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _linkGenerator = linkGenerator;
        }

        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [HttpHead("{userId}")]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = _userRepository.FindById(userId);
            if (user is null)
                return NotFound();
            return Ok(_mapper.Map<UserDto>(user));
        }

        [HttpPost(Name = nameof(CreateUser))]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] CreatedUserDto user)
        {
            if (user is null)
                return BadRequest();
            
            if (user.Login is null || user.Login.Any(loginChar => !char.IsLetterOrDigit(loginChar)))
            {
                ModelState.AddModelError(nameof(user.Login), "Логин должен состоять только из букв и цифр");
            }
            
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            
            var userEntity = _mapper.Map<UserEntity>(user);
            var createdUserEntity = _userRepository.Insert(userEntity);
            
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
        }

        [HttpPut("{userId}", Name = nameof(UpdateUser))]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser([FromBody] UpdatedUserDto user, [FromRoute] Guid userId)
        {
            if (user is null || userId == Guid.Empty)
                return BadRequest();
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            user.Id = userId;
            var updatedUserEntity = _mapper.Map<UserEntity>(user);
            _userRepository.UpdateOrInsert(updatedUserEntity, out var isInserted);
            return isInserted
                ? CreatedAtRoute(
                    nameof(GetUserById),
                    new {userId = updatedUserEntity.Id},
                    updatedUserEntity.Id)
                : NoContent();
        }

        [HttpPatch("{userId}", Name = nameof(PartiallyUpdateUser))]
        public IActionResult PartiallyUpdateUser([FromBody] JsonPatchDocument<UpdatedUserDto> patchDoc, [FromRoute] Guid userId)
        {
            if (patchDoc is null)
                return BadRequest();

            var foundUserEntity = _userRepository.FindById(userId);
            if (foundUserEntity is null)
                return NotFound();

            var user = _mapper.Map<UpdatedUserDto>(foundUserEntity);
            patchDoc.ApplyTo(user, ModelState);
            user.Id = userId;

            if (!TryValidateModel(user) || !ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var userEntityToUpdate = _mapper.Map<UserEntity>(user);
            _userRepository.Update(userEntityToUpdate);

            return NoContent();
        }

        [HttpDelete("{userId}", Name = nameof(DeleteUser))]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            var userEntity = _userRepository.FindById(userId);
            if (userEntity is null)
                return NotFound();
            
            _userRepository.Delete(userId);
            return NoContent();
        }

        [HttpGet(Name = nameof(GetUsers))]
        public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Min(Math.Max(pageSize, 1), 20);
            
            var pageList = _userRepository.GetPage(pageNumber, pageSize);
            var users = _mapper.Map<IEnumerable<UserDto>>(pageList);

            var previousPageLink = pageList.HasPrevious ? 
                _linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new
                {
                    pageNumber = pageNumber - 1,
                    pageSize
                }) 
                : null;
            var nextPageLink = pageList.HasNext ? 
                _linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new
                {
                    pageNumber = pageNumber + 1,
                    pageSize
                }) 
                : null;
            
            var paginationHeader = new
            {
                previousPageLink, 
                nextPageLink,
                totalCount = pageList.TotalCount,
                pageSize,
                currentPage = pageNumber,
                totalPages = pageList.TotalPages,
            };
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
            
            return Ok(users);
        }

        [HttpOptions]
        public IActionResult Options()
        {
            Response.Headers.Add("Allow", "POST, GET, OPTIONS");
            return Ok();
        }
    }
}