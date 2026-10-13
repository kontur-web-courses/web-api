using System.Buffers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");

builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();

builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<CreateUserRequest, UserEntity>();
    cfg.CreateMap<UpdateUserDto, UserEntity>();
    cfg.CreateMap<UserEntity, UserDto>()
        .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.LastName} {src.FirstName}"));
}, new System.Reflection.Assembly[0]);

builder.Services.AddControllers(options =>
{
    // Этот OutputFormatter позволяет возвращать данные в XML, если требуется.
    options.OutputFormatters.Add(new XmlSerializerOutputFormatter());
    // Нужно для того, чтобы XML не стал форматом по умолчанию.
    options.OutputFormatters.Insert(0, new
        NewtonsoftJsonOutputFormatter(new JsonSerializerSettings
        {
            ContractResolver = new
                CamelCasePropertyNamesContractResolver()
        }, ArrayPool<char>.Shared, options));
    // Эта настройка позволяет отвечать кодом 406 Not Acceptable на запросы неизвестных форматов.
    options.ReturnHttpNotAcceptable = true;
    // Эта настройка приводит к игнорированию заголовка Accept, когда он содержит */*
    options.RespectBrowserAcceptHeader = true;
})
    .ConfigureApiBehaviorOptions(options => {
        options.SuppressModelStateInvalidFilter = true;
        options.SuppressMapClientErrors = true;
    })
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Populate;
    });

var app = builder.Build();

app.MapControllers();

app.Run();