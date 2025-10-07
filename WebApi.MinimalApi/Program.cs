using System.Reflection;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json.Serialization;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
        options.SuppressMapClientErrors = true;
    });

builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();

builder.Services.AddControllers(options =>
    {
        options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
        options.ReturnHttpNotAcceptable = true;
        options.RespectBrowserAcceptHeader = true;
    })
    .ConfigureApiBehaviorOptions(o =>
    {
        o.SuppressModelStateInvalidFilter = true;
        o.SuppressMapClientErrors = true;
    })
    .AddNewtonsoftJson(o => { o.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver(); });

builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<UserEntity, UserDto>()
        .ForMember(dest => dest.FullName,
            opt => opt.MapFrom(src => src.FirstName + " " + src.LastName));

    cfg.CreateMap<CreateUserDto, UserEntity>();
    cfg.CreateMap<UpdateUserDto, UserEntity>();
    cfg.CreateMap<UserEntity, UpdateUserDto>();
    cfg.CreateMap<UpdateUserDto, CreateUserDto>();

}, Array.Empty<Assembly>());


var app = builder.Build();

app.MapControllers();

app.Run();