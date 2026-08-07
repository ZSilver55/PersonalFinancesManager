using System.Text.Json.Serialization;
using BudgetManager.Api.Auth;
using BudgetManager.Api.Endpoints;
using BudgetManager.BLL;
using BudgetManager.Commands;
using BudgetManager.Domain;
using BudgetManager.Domain.Enumerations;
using BudgetManager.Queries.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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

// Authentication (JWT from a managed provider — Auth0, Entra External ID, etc.).
// Enabled via config so local/dev can run open until a provider is configured.
bool authEnabled = builder.Configuration.GetValue<bool>("Auth:Enabled");
if (authEnabled)
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = builder.Configuration["Auth:Authority"];
            // Google's id_token audience is the client id, so default the audience to it when
            // Auth:Audience isn't set explicitly (other providers can still override).
            var audience = builder.Configuration["Auth:Audience"];
            options.Audience = string.IsNullOrWhiteSpace(audience)
                ? builder.Configuration["Auth:ClientId"]
                : audience;
            options.TokenValidationParameters.NameClaimType = "sub";
        });

    // Every endpoint requires an authenticated user unless it opts out with AllowAnonymous.
    builder.Services.AddAuthorization(o =>
        o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
}
else
{
    builder.Services.AddAuthorization();
}

// Owns the identity-provider config + secret so thin clients only need the API address.
builder.Services.AddHttpClient<AuthProviderService>();

// Current user resolved from the request token (overrides the default single-user registration).
// Singleton is safe: it holds no per-request state, reading the user from IHttpContextAccessor
// (AsyncLocal) on each access — so singleton stores can depend on it without capturing a scope.
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ICurrentUser, HttpContextCurrentUser>();

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
}).AllowAnonymous();
app.UseCors("web");

if (authEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

// Root + Scalar stay public even when a fallback auth policy is active.
app.MapGet("/", () => Results.Ok(new { service = "BudgetManager API", mode = mode.ToString(), authEnabled }))
   .AllowAnonymous();
app.MapAuthEndpoints();
app.MapBudgetEndpoints();

app.Run();
