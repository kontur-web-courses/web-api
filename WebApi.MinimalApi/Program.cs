var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options => {
        options.SuppressModelStateInvalidFilter = true;
        options.SuppressMapClientErrors = true;
    });

var app = builder.Build();

app.MapControllers();

app.Run();

namespace WebApi.MinimalApi
{
    public partial class Program
    {
        protected Program()
        {
        }
    }
}