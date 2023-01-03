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
            services.AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.SuppressModelStateInvalidFilter = true;
                    options.SuppressMapClientErrors = true;
                });
            services.AddSingleton<IUserRepository, InMemoryUserRepository>();
            services.AddControllers(options =>
            {
                options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
                options.ReturnHttpNotAcceptable = true;
                options.RespectBrowserAcceptHeader = true;
            });
            services.AddAutoMapper(cfg =>
            {
                cfg.CreateMap<UserEntity, UserDto>()
                    .ForMember(
                        dest => dest.FullName,
                        opt => opt.MapFrom(src => $"{src.LastName} {src.FirstName}")
                    );
            }, Array.Empty<Assembly>());
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