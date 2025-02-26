using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFilter("Npgsql", LogLevel.Trace); // Set Npgsql logging level to Debug

// Parse VCAP_SERVICES to get PostgreSQL credentials
var vcapServices = Environment.GetEnvironmentVariable("VCAP_SERVICES");
var postgresCredentials = JsonSerializer.Deserialize<JsonElement>(vcapServices)
    .GetProperty("user-provided")[0]
    .GetProperty("credentials");

var host = postgresCredentials.GetProperty("host").GetString();
var database = postgresCredentials.GetProperty("database").GetString();
var username = postgresCredentials.GetProperty("username").GetString();
var password = postgresCredentials.GetProperty("password").GetString();
var port = postgresCredentials.GetProperty("port").GetString();

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
app.MapGet("/api/version", async (ILogger<Program> logger) =>
{
    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    // Log a statement after establishing the connection
    logger.LogInformation("Database connection established successfully.");

    await using var cmd = new NpgsqlCommand("SELECT version()", conn);
    logger.LogInformation("Query has been successfully constructed");
    var version = await cmd.ExecuteScalarAsync();
    logger.LogInformation("Query executed successfully.");

    return new { Version = version?.ToString() };
});

app.Run();