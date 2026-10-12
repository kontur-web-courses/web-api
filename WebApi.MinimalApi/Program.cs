using System.Buffers;
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
        // Этот OutputFormatter позволяет возвращать данные в XML, если требуется.
        options.OutputFormatters.Add(new XmlSerializerOutputFormatter());
        // Нужно для того, чтобы XML не стал форматом по умолчанию. Можно попробовать убрать и посмотреть, что будет
        options.OutputFormatters.Insert(0, new 
            NewtonsoftJsonOutputFormatter(new JsonSerializerSettings
            {
                ContractResolver = new 
                    CamelCasePropertyNamesContractResolver()
            }, ArrayPool<char>.Shared, options));
        // Эта настройка позволяет отвечать кодом 406 Not Acceptable на запросы неизвестных форматов.
        options.ReturnHttpNotAcceptable = true;
        // Эта настройка приводит к игнорированию заголовка Accept, когда он содержит */*
        // Здесь она нужна, чтобы в этом случае ответ возвращался в формате JSON
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
    cfg.CreateMap<CreateUserDto, UserEntity>();
    
    cfg.CreateMap<UserEntity, UserDto>()
        .ForMember(dest => dest.FullName,opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
   
    cfg.CreateMap<UpdateUserDto, UserEntity>();
    
    cfg.CreateMap<UserEntity, UpdateUserDto>();
}, new System.Reflection.Assembly[0]);

builder.Services.AddSwaggerGeneration();

builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();

var app = builder.Build();

app.UseSwaggerWithUI();

app.MapControllers();

app.Run();
