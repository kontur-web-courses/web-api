using System.Buffers;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
using WebApi.MinimalApi.Samples;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");

builder.Services.AddControllers(options =>
    {
        options.OutputFormatters.Add(new XmlSerializerOutputFormatter());
        options.OutputFormatters.Insert(0, new 
            NewtonsoftJsonOutputFormatter(new JsonSerializerSettings
            {
                ContractResolver = new 
                    CamelCasePropertyNamesContractResolver()
            }, ArrayPool<char>.Shared, options));
        options.ReturnHttpNotAcceptable = true;
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



builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<UserEntity, UserDto>()
        .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.LastName} {src.FirstName}"));
    cfg.CreateMap<UserCreateDto, UserEntity>();
    cfg.CreateMap<UserUpdateDto, UserEntity>();
    cfg.CreateMap<UserEntity, UserUpdateDto>();
}, Array.Empty<Assembly>());

builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
var app = builder.Build();

app.MapControllers();
app.Run();