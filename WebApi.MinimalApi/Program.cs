using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options => {
        options.SuppressModelStateInvalidFilter = true;
        options.SuppressMapClientErrors = true; })
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Populate;
    });

builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();

builder.Services.AddControllers(options =>
    {
        options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
        options.ReturnHttpNotAcceptable = true;
        options.RespectBrowserAcceptHeader = true;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context => new BadRequestObjectResult(context.ModelState);
    });

builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<UserEntity, UserDto>().ForMember(dest => dest.FullName,
        opt => opt.MapFrom(src => src.LastName + " " + src.FirstName));
}, new System.Reflection.Assembly[0]);

builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<UserCreationDto, UserEntity>();
}, new System.Reflection.Assembly[0]);

builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<UpdateDto, UserEntity>();
}, new System.Reflection.Assembly[0]);

builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<UserEntity, UpdateDto>();
}, new System.Reflection.Assembly[0]);

var app = builder.Build();

app.MapControllers();

app.Run();
