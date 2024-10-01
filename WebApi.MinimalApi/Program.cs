using WebApi.MinimalApi.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using WebApi.MinimalApi.Models;

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
});

// Регистрируем репозиторий пользователей в памяти
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();

builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<UserEntity, UserDto>();
}, new System.Reflection.Assembly[0]);

var app = builder.Build();

app.MapControllers();

app.Run();
