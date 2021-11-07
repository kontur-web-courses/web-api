using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    [Produces("application/json", "application/xml")]
    [ApiController]
    public class UsersController : Controller
    {
        private IUserRepository userRepository;
        private IMapper mapper;
        private LinkGenerator linkGenerator;

        public UsersController(IUserRepository userRepository, IMapper mapper, LinkGenerator linkGenerator)
        {
            this.userRepository = userRepository;
            this.mapper = mapper;
            this.linkGenerator = linkGenerator;
        }
        
        [HttpHead("{userId}")]
        [HttpGet("{userId}", Name = nameof(GetUserById))]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var src = userRepository.FindById(userId);
            if (src == null)
                return NotFound();
            var user = mapper.Map<UserEntity, UserDto>(src);
            return Ok(user);
        }
        
        [HttpGet( Name = nameof(GetUsers))]
        public ActionResult<IEnumerable<UserDto>> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            pageSize = pageSize <= 0 ? 1 : pageSize > 20 ? 20 : pageSize;
            
            var pageList = userRepository.GetPage(pageNumber, pageSize);
            var users = mapper.Map<IEnumerable<UserDto>>(pageList);
            
            var prevLink = pageNumber > 1 ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers),  new {pageNumber = pageNumber - 1, pageSize = pageSize}) : null;
            var nextLink = pageNumber < pageList.TotalPages ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers),  new {pageNumber = pageNumber + 1, pageSize = pageSize}) : null;

            var paginationHeader = new
            {
                previousPageLink = prevLink,
                nextPageLink = nextLink,
                totalCount = pageList.TotalCount,
                pageSize = pageSize,
                currentPage = pageNumber,
                totalPages = pageList.TotalPages,
            };
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
            
            return Ok(users);
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] CreateUserDto user)
        {
            if (user == null)
                return BadRequest();
            if (user?.Login != null && !user.Login.ToCharArray().All(char.IsLetterOrDigit))
                ModelState.AddModelError("Login", "Логин должен состоять из букв или цифр");
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            var userEntity = mapper.Map<CreateUserDto, UserEntity>(user);
            var createdUserEntity = userRepository.Insert(userEntity);
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
        }

        [HttpPut("{userId}")]
        public IActionResult UpdateUser([FromRoute] string userId, [FromBody] UpdateUserDto user)
        {
            if (user == null || !Guid.TryParse(userId, out var id))
                return BadRequest();
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);
            var userEntity = mapper.Map<UpdateUserDto, UserEntity>(user, new UserEntity(id));
            userRepository.UpdateOrInsert(userEntity, out var isInserted);
            if (isInserted) 
                return CreatedAtRoute(
                    nameof(GetUserById),
                    new { userId = id },
                    id);
            return NoContent();
        }

        [HttpDelete("{userId}")]
        public IActionResult DeleteUser([FromRoute] string userId)
        {
            if (!Guid.TryParse(userId, out var id))
                return NotFound();
            if (userRepository.FindById(id) == null)
                return NotFound();
            userRepository.Delete(id);
            return NoContent();
        }
    }
}