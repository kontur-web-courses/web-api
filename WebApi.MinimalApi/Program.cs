using System.Reflection;
using Microsoft.AspNetCore.Mvc.Formatters;
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
       .ConfigureApiBehaviorOptions(options =>
       {
           options.SuppressModelStateInvalidFilter = true;
           options.SuppressMapClientErrors = true;
       });
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddAutoMapper(config =>
{
    config.CreateMap<UserEntity, UserDto>()
          .ForMember(dest => dest.FullName,
                     opt => opt.MapFrom(src => $"{src.LastName} {src.FirstName}"));
}, Array.Empty<Assembly>());

var app = builder.Build();

app.MapControllers();

app.Run();