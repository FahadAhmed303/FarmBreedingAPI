using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// OpenAPI (keep)
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ❌ DO NOT use HTTPS redirect on Render
// app.UseHttpsRedirection();

// 🔥 VERSION CONTROL MIDDLEWARE (MUST BE BEFORE CONTROLLERS)
app.Use(async (context, next) =>
{
    var version = context.Request.Headers["app-version"].ToString();

    if (string.IsNullOrEmpty(version) || version != "2")
    {
        context.Response.StatusCode = 426;
        await context.Response.WriteAsync("Update required");
        return;
    }

    await next();
});

app.UseAuthorization();

app.MapControllers();

// Health check (Render needs this)
app.MapGet("/", () => "API is running...");

app.Run();