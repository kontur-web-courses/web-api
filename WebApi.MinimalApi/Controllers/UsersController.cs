using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
using Microsoft.AspNetCore.Routing;



namespace WebApi.MinimalApi.Controllers
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

        [HttpGet("{userId}")]
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
        public IActionResult CreateUser([FromBody] guid user)
        {
            if (user == null)
            {
                return BadRequest();
            }

            if (user.Login == null || !user.Login.All(char.IsLetterOrDigit))
            {
                ModelState.AddModelError("login", "Login must contain only letters and digits and cannot be empty.");
                var response = UnprocessableEntity(ModelState);
                return response;
            }

            var userEntity = mapper.Map<UserEntity>(user);

            userEntity = userRepository.Insert(userEntity);

            return CreatedAtAction(nameof(GetUserById), new { userId = userEntity.Id, login = userEntity.Login }, userEntity.Id);
        }

        [HttpPut("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser([FromRoute] string userId, [FromBody] UpdateUserDto userDto)
        {
            if (userDto == null) return BadRequest();

            var validationResult = ValidateUserDto(userDto);
            if (validationResult != null) return validationResult;

            if (!Guid.TryParse(userId, out Guid guidUserId)) return BadRequest();

            var userEntity = mapper.Map(userDto, new UserEntity(guidUserId));
            userRepository.UpdateOrInsert(userEntity, out var isInsert);

            return isInsert
                ? CreatedAtAction(nameof(GetUserById), new { userId = guidUserId }, userId)
                : NoContent();
        }

        private IActionResult ValidateUserDto(UpdateUserDto userDto)
        {
            if (string.IsNullOrWhiteSpace(userDto.Login) || !userDto.Login.All(char.IsLetterOrDigit))
                return AddModelError("login", "Login must contain only letters and digits and cannot be empty.");

            if (string.IsNullOrWhiteSpace(userDto.FirstName))
                return AddModelError("firstName", "First name cannot be empty.");

            if (string.IsNullOrWhiteSpace(userDto.LastName))
                return AddModelError("lastName", "Last name cannot be empty.");

            return null;
        }

        private IActionResult AddModelError(string fieldName, string message)
        {
            ModelState.AddModelError(fieldName, message);
            return UnprocessableEntity(ModelState);
        }




        [HttpPatch("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult PartiallyUpdateUser([FromRoute] string userId, [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc)
        {
            if (patchDoc == null) return BadRequest();

            if (!Guid.TryParse(userId, out Guid guidUserId))
                return NotFound();

            var user = userRepository.FindById(guidUserId);
            if (user == null)
            {
                return NotFound();
            }

            var updateUserDto = mapper.Map<UpdateUserDto>(user);
            patchDoc.ApplyTo(updateUserDto, ModelState);
            TryValidateModel(updateUserDto);

            return ModelState.IsValid ? NoContent() : UnprocessableEntity(ModelState);
        }





        [HttpDelete("{userId}")]
        public IActionResult DeleteUser([FromRoute] string userId)
        {
            if (!Guid.TryParse(userId, out Guid guidUserId))
            {
                return NotFound();
            }

            var user = userRepository.FindById(guidUserId);
            if (user == null)
            {
                return NotFound();
            }

            userRepository.Delete(user.Id);
            return NoContent();
        }
        
        [HttpHead("{userId}")]
        public IActionResult GetUserById([FromRoute] string userId)
        {
            

            if (!Guid.TryParse(userId, out Guid guidUserId))
            {
                return NotFound();
            }

            var user = userRepository.FindById(guidUserId);
            if (user == null)
            {
                return NotFound();
            }

            Response.ContentType = "application/json; charset=utf-8";
            return Ok();
        }

        [HttpGet]
        [Produces("application/json", "application/xml")]
        public IActionResult GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 1;
            if (pageSize > 20) pageSize = 20;


            var pageList = userRepository.GetPage(pageNumber, pageSize);


            var users = mapper.Map<IEnumerable<UserDto>>(pageList);


            var paginationHeader = new
            {
                previousPageLink = pageList.HasPrevious ?
                    linkGenerator.GetUriByAction(HttpContext, nameof(GetUsers), values: new { pageNumber = pageNumber - 1, pageSize }) : null,
                nextPageLink = pageList.HasNext ?
                    linkGenerator.GetUriByAction(HttpContext, nameof(GetUsers), values: new { pageNumber = pageNumber + 1, pageSize }) : null,
                totalCount = pageList.TotalCount,
                pageSize = pageSize,
                currentPage = pageNumber,
                totalPages = (int)Math.Ceiling((double)pageList.TotalCount / pageSize)
            };

            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

            return Ok(users);
        }



        [HttpOptions]
        public IActionResult Options()
        {
            Response.Headers.Add("Allow", "GET, POST, OPTIONS");

            return Ok();
        }

    }
}
