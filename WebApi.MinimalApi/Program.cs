using System.Reflection;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
using WebApi.MinimalApi.Samples;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddAutoMapper(cfg =>
    {
        cfg.CreateMap<UserEntity, UserDto>();
        cfg.CreateMap<CreateUserDto, UserEntity>();
        cfg.CreateMap<UpdateUserDto, UserEntity>().ReverseMap();
    },
    Array.Empty<Assembly>());
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
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
        options.SuppressMapClientErrors = true;
    })
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Populate;
    });
builder.Services.AddSwaggerGeneration();

var app = builder.Build();
app.UseSwaggerWithUI();

app.MapControllers();

app.Run();