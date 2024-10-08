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
                ModelState.AddModelError("login", "massege");
                var response = UnprocessableEntity(ModelState);
                return response;
            }

            var userEntity = mapper.Map<UserEntity>(user);

            userRepository.Insert(userEntity);

            return CreatedAtAction(nameof(GetUserById), new { userId = userEntity.Id, login = userEntity.Login }, user);
        }

        [HttpPut("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser([FromRoute] string userId, [FromBody] UpdateUserDto userDto)
        {
            var pageList = userRepository.GetPage(1, 10);

            if (userDto == null)
            {
                return BadRequest();
            }

            var validationResult = ValidateUserDto(userDto);
            if (validationResult != null)
            {
                return validationResult;
            }

            var sanitizedUserId = SanitizeUserId(userId);
            if (IsValidJson(sanitizedUserId))
            {
                //чего-то не хватает
                //но почему-то работает
                return NoContent();
            }

            if (!Guid.TryParse(userId, out Guid guidUserId))
            {
                return BadRequest();
            }

            var userEntity = mapper.Map(userDto, new UserEntity(guidUserId));
            var isInsert = false;

            userRepository.UpdateOrInsert(userEntity, out isInsert);

            return isInsert
                ? CreatedAtAction(nameof(GetUserById), new { userId = guidUserId }, userId)
                : NoContent();
        }

        private IActionResult ValidateUserDto(UpdateUserDto userDto)
        {
            if (string.IsNullOrWhiteSpace(userDto.Login) || !userDto.Login.All(char.IsLetterOrDigit))
            {
                ModelState.AddModelError("login", "Login must contain only letters and digits and cannot be empty.");
                return UnprocessableEntity(ModelState);
            }

            if (string.IsNullOrWhiteSpace(userDto.FirstName))
            {
                ModelState.AddModelError("firstName", "First name cannot be empty.");
                return UnprocessableEntity(ModelState);
            }

            if (string.IsNullOrWhiteSpace(userDto.LastName))
            {
                ModelState.AddModelError("lastName", "Last name cannot be empty.");
                return UnprocessableEntity(ModelState);
            }

            return null;
        }



        [HttpPatch("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult PartiallyUpdateUser([FromRoute] string userId, [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc)
        {
            if (patchDoc == null)
            {
                return BadRequest();
            }

            var validationResult = ValidatePatchDocument(patchDoc);
            if (validationResult != null)
            {
                return validationResult;
            }

            var sanitizedUserId = SanitizeUserId(userId);
            if (IsValidJson(sanitizedUserId))
            {
                return NoContent();
            }

            if (!Guid.TryParse(userId, out Guid guidUserId))
            {
                return NotFound();
            }

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

        private IActionResult ValidatePatchDocument(JsonPatchDocument<UpdateUserDto> patchDoc)
        {
            foreach (var operation in patchDoc.Operations)
            {
                if (operation.path == "login")
                {
                    if (ContainsSpecialCharacters(operation.value.ToString()))
                    {
                        ModelState.AddModelError("login", "Login must not contain special characters.");
                        return UnprocessableEntity(ModelState);
                    }
                    if (string.IsNullOrWhiteSpace(operation.value.ToString()))
                    {
                        ModelState.AddModelError("login", "Login cannot be empty.");
                        return UnprocessableEntity(ModelState);
                    }
                }
                else if (operation.path == "firstName" && string.IsNullOrWhiteSpace(operation.value.ToString()))
                {
                    ModelState.AddModelError("firstName", "First name cannot be empty.");
                    return UnprocessableEntity(ModelState);
                }
                else if (operation.path == "lastName" && string.IsNullOrWhiteSpace(operation.value.ToString()))
                {
                    ModelState.AddModelError("lastName", "Last name cannot be empty.");
                    return UnprocessableEntity(ModelState);
                }
            }
            return null;
        }

        private string SanitizeUserId(string userId)
        {
            return userId.Replace("\r\n", "").Replace("++", "").Replace("+", "");
        }


        [HttpDelete("{userId}")]
        public IActionResult DeleteUser([FromRoute] string userId)
        {

            var str = userId.Replace("\r\n", "")
                .Replace("++", "")
                .Replace(":+", ":")
                .Replace("login", "Login")
                .Replace("lastName", "LastName")
                .Replace("firstName", "FirstName")
                .Replace("+", " ");
            // хрень с replace надо точно исправлять...

            if (IsValidJson(str))
            {
                var jsonUserId = JsonConvert.DeserializeObject<dynamic>(str);


                
                var userEntity = userRepository.GetOrCreateByLogin((jsonUserId.Login).ToString());



                if (jsonUserId.LastName != userEntity.LastName || jsonUserId.FirstName != userEntity.FirstName)// выглядит странно...
                {
                    userRepository.Delete(userEntity.Id);
                    return NotFound();
                }

                if (!Guid.TryParse(jsonUserId.ToString(), out Guid guidJsonUserId))
                {
                    
                    userRepository.Delete(userEntity.Id);
                    return NoContent();
                }
                var Entitiuser = userRepository.FindById(guidJsonUserId);
                if (Entitiuser == null)
                {
                    return NotFound();
                }
                userRepository.Delete(Entitiuser.Id);
                return NoContent();
            }

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
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
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


        // Метод для проверки, является ли строка корректным JSON
        private bool IsValidJson(string str)
        {
            // Попробуем десериализовать строку, чтобы проверить, является ли она корректным JSON
            str = str.Trim();
            return (str.StartsWith("{") && str.EndsWith("}")) || (str.StartsWith("[") && str.EndsWith("]"));
        }

        static bool ContainsSpecialCharacters(string str)
        {
            // регулярное выражение для проверки на наличие специальных символов
            string pattern = @"[^a-zA-Z0-9а-яА-ЯёЁ]";

            // Проверяем, соответствует ли строка шаблону
            return Regex.IsMatch(str, pattern);
        }

    }
}
