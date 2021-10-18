using System.Buffers;
using System.Data.SqlTypes;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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
                    // options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
                    options.OutputFormatters.Add(new
                        XmlSerializerOutputFormatter());
                    options.OutputFormatters.Insert(0, new
                        NewtonsoftJsonOutputFormatter(new JsonSerializerSettings
                        {
                            ContractResolver = new
                                CamelCasePropertyNamesContractResolver(),
                            DefaultValueHandling = DefaultValueHandling.Populate
                        }, ArrayPool<char>.Shared, options));
                    options.ReturnHttpNotAcceptable = true;
                    options.RespectBrowserAcceptHeader = true;
                })
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.SuppressModelStateInvalidFilter = true;
                    options.SuppressMapClientErrors = true;
                });
                // .AddNewtonsoftJson(options =>
                // {
                //     options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                //     // optin
                // });
            services.AddSingleton<IUserRepository, InMemoryUserRepository>();
            services.AddAutoMapper(cfg =>
            {
                cfg.CreateMap<UserEntity, UserDto>()
                    .ForMember(dst => dst.FullName,
                        opt => opt.MapFrom(
                            src => $"{src.LastName} {src.FirstName}"));
                cfg.CreateMap<UserCreationDto, UserEntity>();
                cfg.CreateMap<UserUpdateDto, UserEntity>();
                cfg.CreateMap<UserEntity, UserUpdateDto>();
            }, new System.Reflection.Assembly[0]);
            services.AddMvc().AddNewtonsoftJson();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();

            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
