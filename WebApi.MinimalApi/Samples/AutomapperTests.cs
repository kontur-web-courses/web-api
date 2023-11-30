using AutoMapper;
using NUnit.Framework;

namespace WebApi.MinimalApi.Samples;

[TestFixture]
public class AutoMapperTests
{
    class UserEntity
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string Name { get; set; }
        public DateTime RegistrationTime { get; set; }
    }

    class UserDto
    {
        public int Id { get; set; }
        public string VisibleName { get; set; }
        public DateTime RegistrationTime { get; set; }
    }

    class UserToUpdateDto
    {
        public string Login { get; set; }
        public string Name { get; set; }
    }

    IMapper mapper;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var config = new MapperConfiguration(cfg =>
        {
            // Регистрация преобразования UserEntity в UserDto с дополнительным правилом.
            // Также поля и свойства с совпадающими именами будут скопировны (поведение по умолчанию).
            cfg.CreateMap<UserEntity, UserDto>()
                .ForMember(dest => dest.VisibleName, opt => opt.MapFrom(src => src.Name ?? src.Login));

            // Регистрация преобразования UserToUpdateDto в UserEntity без дополнительных правил.
            // Все поля и свойства с совпадающими именами будут скопировны (поведение по умолчанию).
            cfg.CreateMap<UserToUpdateDto, UserEntity>();
        });
        mapper = config.CreateMapper();
    }

    [Test]
    public void TestCreateFrom()
    {
        const int id = 1;
        var registrationTime = DateTime.Now.AddYears(-1);
        var userEntity = new UserEntity
        {
            Id = id,
            Login = "Anonymous",
            Name = null,
            RegistrationTime = registrationTime
        };

        // userDto создается на основе значений из userEntity
        var userDto = mapper.Map<UserDto>(userEntity);

        Assert.AreEqual(id, userDto.Id);
        Assert.AreEqual(registrationTime, userDto.RegistrationTime);
        Assert.AreEqual("Anonymous", userDto.VisibleName);
    }

    [Test]
    public void TestFillBy()
    {
        var registrationTime = DateTime.Now.AddYears(-1);
        var userDto = new UserToUpdateDto
        {
            Login = "SuperAdmin",
            Name = "Jack"
        };

        var userEntity = new UserEntity
        {
            Id = 777,
            Login = "Admin",
            RegistrationTime = registrationTime,
        };

        // userEntity дополняется значениями из userDto
        mapper.Map(userDto, userEntity);

        Assert.AreEqual(777, userEntity.Id);
        Assert.AreEqual("SuperAdmin", userEntity.Login);
        Assert.AreEqual("Jack", userEntity.Name);
        Assert.AreEqual(registrationTime, userEntity.RegistrationTime);
    }

    [Test]
    public void TestFillByReturnSyntax()
    {
        var registrationTime = DateTime.Now.AddYears(-1);
        var userDto = new UserToUpdateDto
        {
            Login = "SuperAdmin",
            Name = "Jack"
        };

        // Mapper.Map возвращает ссылку на второй аргумент.
        // Поэтому можно вторым аргументом передавать только что созданный объект,
        // а потом получать ссылку на него как возвращаемое значение Map.
        var userEntity = mapper.Map(userDto, new UserEntity
        {
            Id = 777,
            Login = "Admin",
            RegistrationTime = registrationTime,
        });

        Assert.AreEqual(777, userEntity.Id);
        Assert.AreEqual("SuperAdmin", userEntity.Login);
        Assert.AreEqual("Jack", userEntity.Name);
        Assert.AreEqual(registrationTime, userEntity.RegistrationTime);
    }

    [Test]
    public void TestCreateMany()
    {
        var userEntities = Enumerable.Range(1, 10)
            .Select(id => new UserEntity
            {
                Id = id
            })
            .ToList();

        // Каждый userDto создается на основе значений из userEntity.
        // Используется преобразование UserEntity в UserDto.
        var userDtos = mapper.Map<IEnumerable<UserDto>>(userEntities).ToList();

        Assert.AreEqual(userEntities.Count, userDtos.Count);
        for (int i = 0; i < userDtos.Count; i++)
            Assert.AreEqual(userEntities[i].Id, userDtos[i].Id);
    }
}