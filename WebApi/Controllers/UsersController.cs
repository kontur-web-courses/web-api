using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        private readonly IUserRepository usersRepository;
        private readonly IMapper mapper;
        private readonly LinkGenerator linkGenerator;

        public UsersController(IUserRepository usersRepository, IMapper mapper, LinkGenerator linkGenerator)
        {
            this.usersRepository = usersRepository;
            this.mapper = mapper;
            this.linkGenerator = linkGenerator;
        }

        [HttpGet("{userId:guid}", Name = nameof(GetUserById))]
        [HttpHead("{userId}")]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = usersRepository.FindById(userId);
            if (user is null)
                return NotFound();

            var userDto = mapper.Map<UserDto>(user);
            return Ok(userDto);
        }

        [HttpPost]
        [Produces("application/json", "application/xml")]
        public IActionResult CreateUser([FromBody] CreateUserDto user)
        {
            if (user is null)
                return BadRequest();

            if (string.IsNullOrEmpty(user.Login) || !user.Login.All(char.IsLetterOrDigit))
            {
                ModelState.AddModelError(nameof(CreateUserDto.Login),
                    "Login should contain only letters or digits");
            }

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var userEntity = mapper.Map<UserEntity>(user);
            var createdUserEntity = usersRepository.Insert(userEntity);
            var response = CreatedAtRoute(
                nameof(GetUserById),
                new {userId = createdUserEntity.Id},
                createdUserEntity.Id);
      
            return response;
        }

        [HttpPut("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdateUserDto user)
        {
            if (user is null || userId == Guid.Empty)
                return BadRequest();
            
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            
            var userEntity = new UserEntity(userId);
            mapper.Map(user, userEntity);
            
            usersRepository.UpdateOrInsert(userEntity, out var isInserted);
            
            return isInserted 
                ? CreatedAtRoute(
                    nameof(GetUserById),
                    new {userId = userEntity.Id},
                    userEntity.Id) 
                : NoContent();
        }
        
        [HttpPatch("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc)
        {
            if (patchDoc is null)
                return BadRequest();

            var userEntity = usersRepository.FindById(userId);
            if (userEntity is null)
                return NotFound();

            var updateDto = mapper.Map<UpdateUserDto>(userEntity);
            patchDoc.ApplyTo(updateDto, ModelState);
            
            TryValidateModel(updateDto);
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            
            mapper.Map(updateDto, userEntity);
            usersRepository.Update(userEntity);

            return NoContent();
        }

        [HttpDelete("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            var user = usersRepository.FindById(userId);
            if (user is null)
                return NotFound();
            
            usersRepository.Delete(userId);
            return NoContent();
        }

        [HttpGet(Name = nameof(GetUsers))]
        [Produces("application/json", "application/xml")]
        public ActionResult<IEnumerable<UserDto>> GetUsers(int pageNumber = 1, int pageSize = 10)
        {
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Min(Math.Max(pageSize, 1), 20);
            var page = usersRepository.GetPage(pageNumber, pageSize);
            
            var paginationHeader = new
            {
                previousPageLink = page.HasPrevious
                    ? CreateGetUsersUri(page.CurrentPage - 1, page.PageSize)
                    : null,
                nextPageLink = page.HasNext
                    ? CreateGetUsersUri(page.CurrentPage + 1, page.PageSize)
                    : null,
                totalCount = page.TotalCount,
                pageSize = page.PageSize,
                currentPage = page.CurrentPage,
                totalPages = page.TotalPages 
            };
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
            
            return Ok(page);
        }
        
        private string CreateGetUsersUri(int pageNumber, int pageSize) 
            => linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), 
                new 
                {
                    pageNumber,
                    pageSize
                });
    }
}