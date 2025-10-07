using System.Reflection;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Создаем документ с описанием API
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Web API",
        Version = "0.1",
    });

    // Конфигурируем Swashbuckle, чтобы использовались Xml Documentation Comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    // Конфигурируем Swashbuckle, чтобы работали атрибуты
    c.EnableAnnotations();
});

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
        .ForMember(x => x.FullName,
            y => y.MapFrom(user => $"{user.LastName} {user.FirstName}"));
    cfg.CreateMap<UserCreateDto, UserEntity>();
    cfg.CreateMap<UserUpdateDto, UserEntity>();
    cfg.CreateMap<UserEntity, UserUpdateDto>();
}, new System.Reflection.Assembly[0]);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Web API");
    c.RoutePrefix = string.Empty;
});

app.MapControllers();

app.Run();