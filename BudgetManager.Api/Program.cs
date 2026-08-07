using System.Text.Json.Serialization;
using BudgetManager.Api.Endpoints;
using BudgetManager.BLL;
using BudgetManager.Commands;
using BudgetManager.Domain;
using BudgetManager.Domain.Enumerations;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Serialize enums as strings (matches the store's JSON convention and reads nicely).
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Bind persistence settings from configuration (so the SQL store gets the connection string).
builder.Services.Configure<Settings>(builder.Configuration.GetSection("Settings"));

// Choose the persistence mode; fall back to JSON if SQL is requested without a connection string.
var requestedMode = builder.Configuration.GetValue<PersistenceMode>("Settings:PersistenceMode");
var connectionString = builder.Configuration.GetValue<string?>("Settings:ConnectionString");
var mode = requestedMode == PersistenceMode.Sql && !string.IsNullOrWhiteSpace(connectionString)
    ? PersistenceMode.Sql
    : PersistenceMode.Json;

builder.Services.AddBudgetPersistence(mode);
builder.Services.AddBudgetApplication();

// CORS for the future Blazor client (origins configurable; "*" allows any during development).
var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "*" };
builder.Services.AddCors(o => o.AddPolicy("web", p =>
{
    if (origins.Length == 1 && origins[0] == "*")
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    else
        p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
}));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Serve the OpenAPI document (also consumed by Scalar) and both UIs.
app.UseSwagger();
app.UseSwaggerUI();
app.MapScalarApiReference(options =>
{
    options.WithTitle("BudgetManager API")
           .WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json");
});
app.UseCors("web");

app.MapGet("/", () => Results.Ok(new { service = "BudgetManager API", mode = mode.ToString() }));
app.MapBudgetEndpoints();

app.Run();
