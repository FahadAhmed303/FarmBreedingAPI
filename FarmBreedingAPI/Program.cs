using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Your existing OpenAPI (DO NOT CHANGE)
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ❌ Disable this for Render
// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Health check (important for Render)
app.MapGet("/", () => "API is running...");

app.Run();