using System.Reflection;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
using WebApi.MinimalApi.Samples;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");
builder.Services
    .AddControllers(options =>
    {
        options.OutputFormatters.Add(new XmlSerializerOutputFormatter());
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
    });

builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();

builder.Services.AddAutoMapper(config =>
{
    config
        .CreateMap<UserEntity, UserDto>()
        .ForMember(
            userDto => userDto.FullName,
            options => options
                .MapFrom(userEntity => $"{userEntity.LastName} {userEntity.FirstName}"));
    config.CreateMap<UserCreationDto, UserEntity>();
    config.CreateMap<UserUpdateDto, UserEntity>();
    config.CreateMap<UserEntity, UserUpdateDto>();
}, Array.Empty<Assembly>());

builder.Services.AddSwaggerGeneration();

var app = builder.Build();
if (app.Environment.IsDevelopment())
    app.UseSwaggerWithUI();

app.MapControllers();
app.Run();