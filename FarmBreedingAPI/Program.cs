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


// 🔥 VERSION CONTROL ONLY (FOCUS ON YOUR REQUIREMENT)
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower();

    // Allow root and login API
    if (path == "/" || path.Contains("/api/auth/login"))
    {
        await next();
        return;
    }

    var version = context.Request.Headers["app-version"].ToString();

    // ❌ BLOCK OLD OR MISSING VERSION
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
app.MapGet("/", () => "API is running...");

app.Run();