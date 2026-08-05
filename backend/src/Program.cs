using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using RetailSystem.Api.Contracts;
using Microsoft.IdentityModel.Tokens;
using RetailSystem.Api.Data;
using RetailSystem.Api.Endpoints;
using RetailSystem.Api.Middleware;
using RetailSystem.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var isOpenApiGeneration = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

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
if (!builder.Environment.IsEnvironment("Testing") && !isOpenApiGeneration)
    builder.Services.AddHostedService<ExpiredOrderCleanupService>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
});
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

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
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ApiResults.WriteAsync(context.Response, StatusCodes.Status401Unauthorized, "登录状态无效或已过期");
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await ApiResults.WriteAsync(context.Response, StatusCodes.Status403Forbidden, "无权执行此操作");
            }
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

if (!isOpenApiGeneration)
    await DatabaseInitializer.InitializeAsync(app.Services);

await app.RunAsync();

public partial class Program;
