using System.Reflection;
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
        options.SuppressMapClientErrors = true;
    })
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Populate;
    });

builder.Services.AddMvc(options =>
{
    options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
    options.RespectBrowserAcceptHeader = true;
    options.ReturnHttpNotAcceptable = true;
});

builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();

builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<UserEntity, UserDto>()
        .ForMember(dest => dest.FullName, 
            opt => opt.MapFrom(src => $"{src.LastName} {src.FirstName}"));
    
    cfg.CreateMap<CreateUserDto, UserEntity>()
        .ForMember(dest => dest.Id, opt => opt.Ignore())
        .ForMember(dest => dest.CurrentGameId, opt => opt.Ignore())
        .ForMember(dest => dest.GamesPlayed, opt => opt.Ignore());
    
    cfg.CreateMap<UpdateUserDto, UserEntity>()
        .ForMember(dest => dest.Id, opt => opt.Ignore())
        .ForMember(dest => dest.CurrentGameId, opt => opt.Ignore())
        .ForMember(dest => dest.GamesPlayed, opt => opt.Ignore());

    cfg.CreateMap<UserEntity, UpdateUserDto>();
}, Array.Empty<Assembly>());

var app = builder.Build();

app.MapControllers();

app.Run();