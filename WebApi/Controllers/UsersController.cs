using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/[controller]")]
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

        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [HttpHead("{userId}")]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(200, "OK", typeof(UserDto))]
        [SwaggerResponse(404, "Пользователь не найден")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId); 

            if (user == null)
            {
                return NotFound();
            }


            return Ok(mapper.Map<UserDto>(user));
        }

        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(201, "Пользователь создан")]
        [SwaggerResponse(400, "Некорректные входные данные")]
        [SwaggerResponse(422, "Ошибка при проверке")]
        public IActionResult CreateUser([FromBody] NewUserData user)
        {
            if (user == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }


            var userToSave = mapper.Map<UserEntity>(user);

            var savedUser = userRepository.Insert(userToSave);

            return CreatedAtRoute(
                nameof(GetUserById),
                new {userId = savedUser.Id},
                savedUser.Id);
        }

        [HttpPut("{userId}")]
        [Consumes("application/json")]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(201, "Пользователь создан")]
        [SwaggerResponse(204, "Пользователь обновлен")]
        [SwaggerResponse(400, "Некорректные входные данные")]
        [SwaggerResponse(422, "Ошибка при проверке")]
        public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdatingUserData user)
        {
            if (user == null)
            {
                return BadRequest();
            }

            if (userId == Guid.Empty)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            var userEntity = new UserEntity(userId);
            var userToSave = mapper.Map(user, userEntity);

            bool isInserted;
            userRepository.UpdateOrInsert(userToSave, out isInserted);

            if (isInserted)
            {
                return CreatedAtRoute(
                    nameof(GetUserById),
                    new {userId = userToSave.Id},
                    userToSave.Id);
            }

            return NoContent();
        }

        [HttpPatch("{userId}")]
        [Produces("application/json")]
        public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdatingUserData> patchDoc)
        {
            if (patchDoc == null)
            {
                return BadRequest();
            }
            
            if (userId == Guid.Empty)
                return NotFound();
            
            var user = new UpdatingUserData();
            patchDoc.ApplyTo(user, ModelState);
            
            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }
            
            if (userRepository.FindById(userId) == null)
            {
                return NotFound();
            }

            if (!TryValidateModel(user))
            {
                return UnprocessableEntity(ModelState);
            }

            var userEntity = new UserEntity(userId);
            var userToSave = mapper.Map(user, userEntity);
            userRepository.Update(userToSave);
            return NoContent();
        }

        [HttpDelete("{userId}")]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(204, "Пользователь удален")]
        [SwaggerResponse(404, "Пользователь не найден")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            if (userId == Guid.Empty)
                return NotFound();
            
            if (userRepository.FindById(userId) == null)
            {
                return NotFound();
            }
            
            userRepository.Delete(userId);

            return NoContent();
        }

        [HttpGet(Name = nameof(GetUsers))]
        [Produces("application/json", "application/xml")]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
        public IActionResult GetUsers([FromQuery(Name = "pageNumber")] int pageNumber = 1, [FromQuery(Name = "pageSize")] int pageSize = 10)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Max(1, pageSize);
            pageSize = Math.Min(20, pageSize);
            
            var pageList = userRepository.GetPage(pageNumber, pageSize);
            var paginationHeader = new
            {
                previousPageLink = pageNumber != 1
                    ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new
                    {
                        pageNumber = pageNumber - 1,
                        pageSize
                    }) : null,
                nextPageLink = pageNumber != pageList.TotalPages
                    ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new
                    {
                        pageNumber = pageNumber + 1,
                        pageSize
                    }) : null,
                totalCount = pageList.TotalCount,
                pageSize = pageList.PageSize,
                currentPage = pageList.CurrentPage,
                totalPages = pageList.TotalPages,
            };
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

            var users = mapper.Map<IEnumerable<UserDto>>(pageList);
            return Ok(users);
        }

        [HttpOptions]
        [SwaggerResponse(200, "OK")]
        public IActionResult OptionsUsers()
        {
            Response.Headers.Add("Allow", new [] {"POST", "GET", "OPTIONS"});
            return Ok();
        }
        
    }
    
}