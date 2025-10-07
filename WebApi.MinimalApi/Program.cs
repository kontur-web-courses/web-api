using System.Reflection;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");
builder.Services.AddControllers(options =>
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
        options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Populate;
    });

builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<UserEntity, UserDto>()
        .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id ))
        .ForMember(dest => dest.Login, opt => opt.MapFrom(src => src.Login))
        .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.LastName} {src.FirstName}"))
        .ForMember(dest => dest.CurrentGameId, opt => opt.MapFrom(src => src.CurrentGameId));

    cfg.CreateMap<UserForCreateDto, UserEntity>()
        .ForMember(dest => dest.Login, opt => opt.MapFrom(src => src.Login))
        .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
        .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
        .ForMember(dest => dest.GamesPlayed, opt => opt.MapFrom(src => 0));
    
    cfg.CreateMap<UserForPutDto, UserEntity>()
        .ForMember(dest => dest.Login, opt => opt.MapFrom(src => src.Login))
        .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
        .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
        .ForMember(dest => dest.GamesPlayed, opt => opt.MapFrom(src => 0));

    cfg.CreateMap<UserEntity, UserToUpdateDto>()
        .ForMember(dest => dest.Login, opt => opt.MapFrom(src => src.Login))
        .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
        .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName));
}, Array.Empty<Assembly>());

builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();

builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations(); 
    
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename)); 
});

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapControllers();

app.UseSwagger();

app.UseSwaggerUI();

app.Run();