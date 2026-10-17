using WebApi.MinimalApi.Domain;
using Microsoft.AspNetCore.Mvc.Formatters;
using WebApi.MinimalApi.Models;
using System.Reflection;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");

builder.Services.AddControllers(options =>
    {
        options.ReturnHttpNotAcceptable = true;
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
    })
    .AddXmlSerializerFormatters();

builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<UserEntity, UserDto>()
        .ForMember(
            dest => dest.FullName,
            opt => opt.MapFrom(
                src => $"{src.LastName} {src.FirstName}"
            )
        );
    cfg.CreateMap<AddUserDto, UserEntity>();
    cfg.CreateMap<UpdateUserDto, UserEntity>();
}, Array.Empty<Assembly>());

var app = builder.Build();

app.MapControllers();

app.Run();