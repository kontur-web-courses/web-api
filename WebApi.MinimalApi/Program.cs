using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using Microsoft.OpenApi.Models;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
using WebApi.MinimalApi.Samples;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");
builder.Services.AddControllers(options =>
    {
        // Этот OutputFormatter позволяет возвращать данные в XML, если требуется.
        options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
        // Эта настройка позволяет отвечать кодом 406 Not Acceptable на запросы неизвестных форматов.
        options.ReturnHttpNotAcceptable = true;
        // Эта настройка приводит к игнорированию заголовка Accept, когда он содержит */*
        // Здесь она нужна, чтобы в этом случае ответ возвращался в формате JSON
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
    cfg.CreateMap<UserEntity, UserDto>()
        .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.LastName} {src.FirstName}"));
    cfg.CreateMap<CreateUserDto, UserEntity>();
    cfg.CreateMap<UpdateUserDto, UserEntity>();
    cfg.CreateMap<UserEntity, UpdateUserDto>();

}, Array.Empty<Assembly>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" }); });

var app = builder.Build();

app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"));
app.Run();