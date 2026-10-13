using System.Buffers;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");

builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddAutoMapper(config => config.Setup(), Array.Empty<Assembly>());

builder.Services
    .AddControllers(options =>
    {
        options.OutputFormatters.Add(new XmlSerializerOutputFormatter());
        options.OutputFormatters.Insert(0, new NewtonsoftJsonOutputFormatter(
            new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() },
            ArrayPool<char>.Shared,
            options,
            null));
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

var app = builder.Build();

app.MapControllers();

app.Run();