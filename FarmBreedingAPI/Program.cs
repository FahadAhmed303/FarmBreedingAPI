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


// 🔥 LOGIN + VERSION CONTROL (FINAL - DO NOT CHANGE AGAIN)
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower();

    // ✅ Allow health check
    if (path == "/")
    {
        await next();
        return;
    }

    // ✅ Allow login API
    if (path != null && path.Contains("/api/auth/login"))
    {
        await next();
        return;
    }

    // ❌ BLOCK if not logged in
    var isLoggedIn = context.Request.Headers["isLoggedIn"].ToString();
    if (isLoggedIn != "true")
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Login required");
        return;
    }

    // ❌ BLOCK if wrong version
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

// Health check (important for Render)
app.MapGet("/", () => "API is running...");

app.Run();