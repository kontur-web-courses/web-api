using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WebApi
{
    using System;
    using AutoMapper;
    using Game.Domain;
    using Microsoft.AspNetCore.Mvc.Formatters;
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

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
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Populate;
                });

            services.AddSingleton<IUserRepository, InMemoryUserRepository>();

            services.AddAutoMapper(cfg =>
            {
                cfg.CreateMap<UserEntity, UserDto>();
                cfg.CreateMap<CreateUserDto, UserEntity>();
                cfg.CreateMap<PutUserDto, UserEntity>()
                    .ConstructUsing((dto, context) =>
                    {
                        var items = context.Options.Items;
                        return new UserEntity(
                            (Guid)items["Id"],
                            dto.Login,
                            dto.LastName,
                            dto.FirstName,
                            0,
                            null
                        );
                    });
                cfg.CreateMap<UserEntity, UpdateDto>().ReverseMap();
            });
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