using System.Buffers;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
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
           options.OutputFormatters.Add(new NewtonsoftJsonOutputFormatter(
                                            new JsonSerializerSettings
                                            {
                                                ContractResolver = new
                                                    CamelCasePropertyNamesContractResolver()
                                            },
                                            ArrayPool<char>.Shared,
                                            options,
                                            new MvcNewtonsoftJsonOptions()));
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
builder.Services.AddAutoMapper(config =>
{
    config.CreateMap<UserEntity, UserDto>()
          .ForMember(dest => dest.FullName,
                     opt => opt.MapFrom(src => $"{src.LastName} {src.FirstName}"));
    config.CreateMap<UserEntity, UserToUpdateDto>();
    config.CreateMap<UserToCreateDto, UserEntity>();
    config.CreateMap<UserToUpdateDto, UserEntity>();
}, Array.Empty<Assembly>());
builder.Services.AddSwaggerGeneration();

var app = builder.Build();

app.MapControllers();
app.UseSwaggerWithUI();

app.Run();