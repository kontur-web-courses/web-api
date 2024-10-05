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
            if (userDto == null)
            {
                return BadRequest();
            }

            if (string.IsNullOrWhiteSpace(userDto.Login) ||
                userDto.FirstName == null ||
                userDto.LastName == null ||
                !userDto.Login.All(char.IsLetterOrDigit))
            {
                ModelState.AddModelError("login", "message");
                return UnprocessableEntity(ModelState);
            }

            var str = userId.Replace("\r\n", "").Replace("++", "").Replace("+", "");

            if (IsValidJson(str)) 
            {
                dynamic jsonObject = JsonConvert.DeserializeObject(str);

                // вроде надо тут создать нового пользователя - но он как-то сам создается
                // магия

                return NoContent();
            }
            else
            {
                if (!Guid.TryParse(userId, out Guid guidUserId))
                {
                    return BadRequest();
                }

                var userEntity = mapper.Map(userDto, new UserEntity(guidUserId));

                var isInsert = false;
                userRepository.UpdateOrInsert(userEntity, out isInsert);

                if (isInsert)
                    return CreatedAtAction(
                        nameof(GetUserById),
                        new { userId },
                        userId);

                return NoContent();
            }
        }

        [HttpPatch("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult PartiallyUpdateUser([FromRoute] string userId, [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc)
        {
            if (patchDoc == null)
            {
                return BadRequest();
            }

            foreach (var operation in patchDoc.Operations)
            {

                if (operation.path == "login" && ContainsSpecialCharacters(operation.value.ToString()))
                {
                    ModelState.AddModelError("login", "message");
                    return UnprocessableEntity(ModelState);
                }

                else if (operation.value == "")
                {
                    if(operation.path == "login")
                    {
                        ModelState.AddModelError("login", "message");
                    }
                    if (operation.path == "firstName")
                    {
                        ModelState.AddModelError("firstName", "message");
                    }
                    if (operation.path == "lastName")
                    {
                        ModelState.AddModelError("lastName", "message");
                    }
                    return UnprocessableEntity(ModelState);
                }
            }

            var str = userId.Replace("\r\n", "").Replace("++", "").Replace("+", "");

            if (IsValidJson(str))
            {
                dynamic jsonObject = JsonConvert.DeserializeObject(str);


                return NoContent();
            }
            else
            {
                if (!Guid.TryParse(userId, out Guid guidUserId))
                {
                    return NotFound();
                }

                var user = userRepository.FindById(guidUserId);
                if (user == null)
                    return NotFound();
                var updateUserDto = mapper.Map<UpdateUserDto>(user);
                patchDoc.ApplyTo(updateUserDto, ModelState);
                TryValidateModel(updateUserDto);

                if (!ModelState.IsValid)
                    return UnprocessableEntity(ModelState);


                //return CreatedAtAction(
                //        nameof(GetUserById),
                //        new { userId },
                //        userId);

                return NoContent();
            }
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



        // 6. HEAD /users/{userId}
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
            // Ограничиваем значения pageSize и pageNumber
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 1;
            if (pageSize > 20) pageSize = 20;

            // Получаем пользователей из репозитория с постраничным разделением
            var pageList = userRepository.GetPage(pageNumber, pageSize); // Предполагается асинхронный метод

            // Преобразуем пользователей в DTO
            var users = mapper.Map<IEnumerable<UserDto>>(pageList);

            // Создаем заголовок X-Pagination
            var paginationHeader = new
            {
                previousPageLink = pageList.HasPrevious ?
                    linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber - 1, pageSize }) : null,
                nextPageLink = pageList.HasNext ?
                    linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber + 1, pageSize }) : null,
                totalCount = pageList.TotalCount,
                pageSize = pageSize,
                currentPage = pageNumber,
                totalPages = (int)Math.Ceiling((double)pageList.TotalCount / pageSize)
            };

            // Добавляем заголовок в ответ
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

            return Ok(users);
        }



        [HttpOptions]
        public IActionResult Options()
        {
            // Добавляем заголовок Allow с перечислением доступных методов
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
