using System.Reflection;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json.Serialization;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
using WebApi.MinimalApi.Samples;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");
builder.Services
    .AddControllers(options =>
    {
        options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
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
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<UserEntity, UserDto>()
        .ForMember(u => u.FullName, 
            opt => opt.MapFrom(u => $"{u.LastName} {u.FirstName}"));
    cfg.CreateMap<CreateUserRequest, UserEntity>();
    cfg.CreateMap<UpsertUserRequest, UserEntity>();
    cfg.CreateMap<UserEntity, PartiallyUpdateUserRequest>().ReverseMap();
}, Array.Empty<Assembly>());
builder.Services.AddSwaggerGeneration();

var app = builder.Build();

app.UseSwaggerWithUI();
app.MapControllers();

app.Run();