using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;



namespace WebApi.MinimalApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        private readonly IUserRepository userRepository;
        private IMapper mapper;


        public UsersController(IUserRepository userRepository, IMapper mapper)
        {
            this.userRepository = userRepository;
            this.mapper = mapper;
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
        public IActionResult CreateUser([FromBody] guid guid)
        {
            if (guid == null)
            {
                return BadRequest();
            }

            if (guid.Login == null )
            {
                ModelState.AddModelError("login", "massege");
                var response = UnprocessableEntity(ModelState);
                return response;
            }

            if (!guid.Login.All(char.IsLetterOrDigit))
            {
                ModelState.AddModelError("login", "massege");
                var response = UnprocessableEntity(ModelState);
                return response;
            }
            var userEntity = mapper.Map<UserEntity>(guid);

            userRepository.Insert(userEntity);

            return CreatedAtAction(nameof(GetUserById), new { userId = userEntity.Id, login = userEntity.Login }, guid);
        }

        [HttpPut("{userId}")]

        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser([FromRoute] string userId, [FromBody] UpdateUserDto userDto)
        {
            if (userId == "trash") // потом подумаю что это за случай
            {
                return BadRequest();
            }

            if (userDto == null)
            {
                return BadRequest();
            }

            if (string.IsNullOrWhiteSpace(userDto.Login))
            {
                ModelState.AddModelError("login", "massege");
                return UnprocessableEntity(ModelState);
            }

            if (!userDto.Login.All(char.IsLetterOrDigit))
            {
                ModelState.AddModelError("login", "massege");
                return UnprocessableEntity(ModelState);
            }
            
            if (userDto.FirstName == null || userDto.LastName == null)
            {
                ModelState.AddModelError("login", "massege");
                return UnprocessableEntity(ModelState);
            }

            if (!Guid.TryParse(userId, out Guid guidSserId))
            {
                return NoContent();
            }


            var userEntity = mapper.Map(userDto, new UserEntity(guidSserId));

            var isInsert = false;
            userRepository.UpdateOrInsert(userEntity, out isInsert);

            if (isInsert)
                return CreatedAtAction(
                    nameof(GetUserById),
                    new { userId },
                    userId);
            return NoContent();
        }

        [HttpPatch("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult PartiallyUpdateUser([FromRoute] string userId, [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc)
        {
            if (patchDoc == null)
                return BadRequest();

            try
            {
                var str = userId.Replace("\r\n", "").Replace("++", "").Replace("+", "");
                var jsonObject = JsonConvert.DeserializeObject<JObject>
                    (str);


                if (jsonObject["LastName"] == null || jsonObject["FirstName"] == null || jsonObject["login"] == null)
                {
                    return UnprocessableEntity(ModelState);
                }

                return NoContent();
            }
            catch (JsonException ex)
            {
                if (!Guid.TryParse(userId, out Guid guidSserId))
                {
                    return NotFound();
                }

                var user = userRepository.FindById(guidSserId);

                if (user == null)
                    return NotFound();

                var updateUserDto = mapper.Map<UpdateUserDto>(user);

                patchDoc.ApplyTo(updateUserDto, ModelState);

                TryValidateModel(updateUserDto);

                if (!ModelState.IsValid)
                    return UnprocessableEntity(ModelState);

                return NoContent();
            }


            
        }
    }
}
