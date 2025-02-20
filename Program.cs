using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

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
app.MapGet("/api/version", async () =>
{
    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    await using var cmd = new NpgsqlCommand("SELECT version()", conn);
    var version = await cmd.ExecuteScalarAsync();

    return new { Version = version?.ToString() };
});

app.Run();