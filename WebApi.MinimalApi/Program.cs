var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("https://localhost:5001;http://localhost:5000");
builder.WebHost.UseEnvironment("Development");
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();