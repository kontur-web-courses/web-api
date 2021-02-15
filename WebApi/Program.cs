using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }
        
        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    // NOTE: Жесткий способ настройки, который сработает в 100% различных IDE.
                    // Для продакшена следует использовать аргументы командной строки,
                    // переменные окружения, файлы конфигурации
                    webBuilder.UseUrls("https://localhost:5001;http://localhost:5000");
                    webBuilder.UseEnvironment("Development");

                    webBuilder.UseStartup<Startup>();
                });
    }
}
