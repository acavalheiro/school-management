using System.Text;
using Api.Endpoints;
using Api.Extensions;
using Application;
using Application.Common;
using Infrastructure;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// --- Services ---
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                  ?? throw new InvalidOperationException($"Missing '{JwtSettings.SectionName}' configuration section.");
jwtSettings.Validate();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppRoles.SuperAdmin, policy => policy.RequireRole(AppRoles.SuperAdmin));
    options.AddPolicy(AppRoles.Admin, policy => policy.RequireRole(AppRoles.Admin));
});

// CORS for React frontend
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

// --- Middleware ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapDefaultEndpoints();

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// --- Endpoints ---
app.MapAuthEndpoints();
app.MapStudentEndpoints();
app.MapUserManagementEndpoints();
app.MapTenantEndpoints();

// --- Startup tasks ---
if (app.Environment.IsDevelopment())
{
    await app.ApplyMigrationsAsync();
    await app.SeedRolesAsync();
}

app.Run();

// Needed for WebApplicationFactory in integration tests
public partial class Program;
