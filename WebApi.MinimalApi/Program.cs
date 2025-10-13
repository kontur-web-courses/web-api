using System.Text.Json;
using AutoMapper;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("https://localhost:5001");

// Настройка DI для репозитория
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();

// Настройка AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<UserEntity, UserDto>()
        .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.LastName} {src.FirstName}"));
    cfg.CreateMap<CreateUserDto, UserEntity>();
    cfg.CreateMap<UpdateUserDto, UserEntity>();
    cfg.CreateMap<UserEntity, UpdateUserDto>();
}, new System.Reflection.Assembly[0]);

// Настройка контроллеров с поддержкой XML и валидации
builder.Services.AddControllers(options =>
{
    // Этот OutputFormatter позволяет возвращать данные в XML, если требуется.
    options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
    // Эта настройка позволяет отвечать кодом 406 Not Acceptable на запросы неизвестных форматов.
    options.ReturnHttpNotAcceptable = true;
    // Эта настройка приводит к игнорированию заголовка Accept, когда он содержит */*
    // Здесь она нужна, чтобы в этом случае ответ возвращался в формате JSON
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