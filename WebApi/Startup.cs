using System;
using System.Reflection;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebApi.Models;

namespace WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers(options =>
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
                    });
            services.AddSingleton<IUserRepository, InMemoryUserRepository>();

            services.AddAutoMapper(typeof(Mappers));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();

            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}