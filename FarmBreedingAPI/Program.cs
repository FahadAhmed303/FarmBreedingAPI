
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

builder.Host.ConfigureHostOptions(o =>
{
    o.ServicesStartConcurrently = false;
});

// Controllers
builder.Services.AddControllers();

// OpenAPI (keep)

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
}

app.UseDefaultFiles();
app.UseStaticFiles();

// DO NOT use HTTPS redirect on Render
// app.UseHttpsRedirection();

// VERSION CONTROL - API REQUESTS ONLY
app.Use(async (context, next) =>
{
    var path = context.Request.Path;

    // Apply version control only to API requests
    if (!path.StartsWithSegments("/api"))
    {
        await next();
        return;
    }

    // Allow login API
    if (path.StartsWithSegments("/api/auth/login"))
    {
        await next();
        return;
    }

    var version = context.Request.Headers["app-version"].ToString();

    // Block old or missing application versions
    if (string.IsNullOrEmpty(version) || version != "2")
    {
        context.Response.StatusCode = 426;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(
            "{\"message\":\"Version is old. Please update your application.\"}"
        );

        return;
    }

    await next();
});

app.UseAuthorization();

app.MapControllers();

// Health check
app.MapGet("/health", () => "API is running...");

app.Run();
