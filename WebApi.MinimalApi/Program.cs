using WebApi.MinimalApi.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using WebApi.MinimalApi.Models;
using System;
using static WebApi.MinimalApi.Controllers.UsersController;
using Newtonsoft.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");

// Добавляем контроллеры с настройками для поддержки JSON и XML
builder.Services.AddControllers(options =>
{
    // Добавляем поддержку XML
    options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());

    // Возвращать код 406 Not Acceptable для неподдерживаемых форматов
    options.ReturnHttpNotAcceptable = true;

    // Игнорируем заголовок Accept, если он содержит */*
    options.RespectBrowserAcceptHeader = true;
})
.ConfigureApiBehaviorOptions(options => {
    options.SuppressModelStateInvalidFilter = true;
    options.SuppressMapClientErrors = true;
})
.AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
}); 

// Регистрируем репозиторий пользователей в памяти
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();

builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<UserDto, UserEntity>();
    cfg.CreateMap<UserDto, guid>();
    cfg.CreateMap<UserEntity, guid>();
    cfg.CreateMap<UserEntity, UserDto>();
    cfg.CreateMap<guid, UserEntity>();
}, new System.Reflection.Assembly[0]);



var app = builder.Build();

app.MapControllers();

app.Run();
