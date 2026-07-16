using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RetailSystem.Api.Data;
using RetailSystem.Api.Endpoints;
using RetailSystem.Api.Middleware;
using RetailSystem.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var jwtSecretFromEnvironment = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
if (!string.IsNullOrWhiteSpace(jwtSecretFromEnvironment)) jwtOptions.SecretKey = jwtSecretFromEnvironment;
if (Encoding.UTF8.GetByteCount(jwtOptions.SecretKey) < 32)
    throw new InvalidOperationException("JWT secret must contain at least 32 bytes");

builder.Services.Configure<JwtOptions>(options =>
{
    options.Issuer = jwtOptions.Issuer;
    options.Audience = jwtOptions.Audience;
    options.SecretKey = jwtOptions.SecretKey;
    options.ExpirationMinutes = jwtOptions.ExpirationMinutes;
});

var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("Oracle")
    ?? "User Id=retail_user;Password=retail_pass;Data Source=localhost:1521/XEPDB1;";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(
        connectionString,
        oracleOptions => oracleOptions.UseOracleSQLCompatibility(
            OracleSQLCompatibility.DatabaseVersion21)));
builder.Services.AddScoped<JwtService>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.Name
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:3000"])
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddOpenApi("v1");
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi("/openapi/{documentName}.json");
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/openapi/v1.json")).ExcludeFromDescription();

app.MapAuthEndpoints();
app.MapProductEndpoints();
app.MapCartEndpoints();
app.MapOrderEndpoints();
app.MapMerchantEndpoints();
app.MapTicketEndpoints();
app.MapReportEndpoints();
app.MapAdminEndpoints();

if (Assembly.GetEntryAssembly()?.GetName().Name != "GetDocument.Insider")
    await DatabaseInitializer.InitializeAsync(app.Services);

await app.RunAsync();

public partial class Program;
