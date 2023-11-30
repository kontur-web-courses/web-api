using System.Reflection;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Swagger;

namespace WebApi.MinimalApi.Samples;

public static class SwaggerExtensions
{
    public static void AddSwaggerGeneration(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            // Создаем документ с описанием API
            c.SwaggerDoc("web-api", new OpenApiInfo
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
    }

    public static void UseSwaggerWithUI(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/web-api/swagger.json", "Web API");
            c.RoutePrefix = string.Empty;
        });
    }

    public static string GetSwaggerDocument(this IWebHost host, string documentName)
    {
        var sw = (ISwaggerProvider)host.Services.GetService(typeof(ISwaggerProvider));
        var doc = sw.GetSwagger(documentName);

        return JsonConvert.SerializeObject(
            doc,
            Formatting.Indented,
            new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
            });
    }
}