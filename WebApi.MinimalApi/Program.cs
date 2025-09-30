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
    .ConfigureApiBehaviorOptions(options => {
        options.SuppressModelStateInvalidFilter = true;
        options.SuppressMapClientErrors = true;
    });

builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<UserEntity, UserDto>()
        .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id ))
        .ForMember(dest => dest.Login, opt => opt.MapFrom(src => src.Login))
        .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.LastName} {src.FirstName}"))
        .ForMember(dest => dest.CurrentGameId, opt => opt.MapFrom(src => src.CurrentGameId));
}, new System.Reflection.Assembly[0]);

builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();


var app = builder.Build();

app.MapControllers();

app.Run();