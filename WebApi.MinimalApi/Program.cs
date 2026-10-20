using WebApi.MinimalApi.Domain;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.MinimalApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");

// регаем репозиторий юзера
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();

// AutoMapper настраиваем
builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<UserEntity, UserDto>()
       .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.LastName} {src.FirstName}"));
       cfg.CreateMap<UserEntity, UpdateDto>().ReverseMap(); // это для UpdateDto
       cfg.CreateMap<CreateUserDto, UserEntity>();
}, new System.Reflection.Assembly[0]);


// контроллеры, JSON, XML и статус 406

builder.Services
    .AddControllers(options =>
    {
        // Возвращать 406 для неподдерживаемого формата.
        options.ReturnHttpNotAcceptable = true;

        // Учитывать заголовок Accept.
        options.RespectBrowserAcceptHeader = true;
    })
    .AddNewtonsoftJson(options =>
    {
        // Сохраняем CamelCase для JSON.
        options.SerializerSettings.ContractResolver =
            new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
    })
    .AddXmlSerializerFormatters()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
        options.SuppressMapClientErrors = true;
    });

var app = builder.Build();

app.MapControllers();

app.Run();