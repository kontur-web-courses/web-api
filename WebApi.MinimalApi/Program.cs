using System.Buffers;
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
        options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Populate;
    });;
builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<UserEntity, UserDto>()
        .ForMember(dest => dest.FullName, opt 
            => opt.MapFrom(src => $"{src.LastName} {src.FirstName}"));

    cfg.CreateMap<UserCreateRequest, UserEntity>();

    cfg.CreateMap<UserEntity, UpdateDto>();
    cfg.CreateMap<UpdateDto, UserEntity>();
    
    cfg.CreateMap<UserUpdateRequest, UserEntity>()
        .ForCtorParam("id", opt =>
            opt.MapFrom((_, context) => context.Items["Id"]));

}, Array.Empty<Assembly>());
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
var app = builder.Build();

app.MapControllers();

app.Run();