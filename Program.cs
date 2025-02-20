using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file
Env.Load();

// Construct the connection string using environment variables
var host = Environment.GetEnvironmentVariable("POSTGRES_HOST");
var database = Environment.GetEnvironmentVariable("POSTGRES_DATABASE");
var username = Environment.GetEnvironmentVariable("POSTGRES_USERNAME");
var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
var port = Environment.GetEnvironmentVariable("POSTGRES_PORT");

var connectionString = $"Host={host};Database={database};Username={username};Password={password};Port={port}";

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

// Add an API endpoint to fetch PostgreSQL version
app.MapGet("/api/version", async () =>
{
    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    await using var cmd = new NpgsqlCommand("SELECT version()", conn);
    var version = await cmd.ExecuteScalarAsync();

    return new { Version = version?.ToString() };
});

app.Run();