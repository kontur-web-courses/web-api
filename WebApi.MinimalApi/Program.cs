using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
using WebApi.MinimalApi.Models.Requests;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5001");
builder.Services.AddControllers(options =>
    {
        options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
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
builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<UserEntity, UpdateUserRequest>();
    cfg.CreateMap<UserEntity, UserDto>()
        .ForMember(dto => dto.FullName,
            opt => opt.MapFrom(user => $"{user.LastName} {user.FirstName}"));
    cfg.CreateMap<CreateUserRequest, UserEntity>();
    cfg.CreateMap<UpdateUserRequest, UserEntity>();
}, Array.Empty<Assembly>());

var app = builder.Build();

app.UseRouting();
app.MapGet("/test", () => "Test");
app.MapControllers();

app.Run();