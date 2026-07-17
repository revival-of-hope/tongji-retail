using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RetailSystem.Backend.Data;
using RetailSystem.Backend.Services;

namespace RetailSystem.Backend;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var jwtOptions = builder.Configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>() ?? new JwtOptions();
        var jwtSecretFromEnvironment = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
        if (!string.IsNullOrWhiteSpace(jwtSecretFromEnvironment))
        {
            jwtOptions.SecretKey = jwtSecretFromEnvironment;
        }

        ValidateJwtOptions(jwtOptions);
        builder.Services.Configure<JwtOptions>(options =>
        {
            options.Issuer = jwtOptions.Issuer;
            options.Audience = jwtOptions.Audience;
            options.SecretKey = jwtOptions.SecretKey;
            options.ExpirationMinutes = jwtOptions.ExpirationMinutes;
        });
        builder.Services.AddScoped<JwtService>();
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.Name
                };
            });
        builder.Services.AddAuthorization();

        // 注册 Oracle 数据库上下文，连接串可用环境变量覆盖。
        var oracleConnection = builder.Configuration.GetConnectionString("OracleConnection")
            ?? throw new InvalidOperationException("未配置 OracleConnection 数据库连接字符串。");
        builder.Services.AddDbContext<RetailDbContext>(options =>
            options.UseOracle(oracleConnection));

        // 注册接口文档和本地联调所需的跨域策略。
        builder.Services.AddOpenApi();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("LocalDevelopment", policy =>
            {
                policy
                    .WithOrigins(
                        "http://localhost:3000",
                        "http://127.0.0.1:3000",
                        "http://localhost:5140",
                        "https://localhost:7108")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseCors("LocalDevelopment");
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/", () => Results.Ok(new ApiResponse<object>(
            true,
            "backend3: 后端服务运行中",
            new { })));

        // 按业务模块组织路由，后续可逐步替换为真实处理逻辑。
        var auth = app.MapGroup("/api/auth").WithTags("认证");
        auth.MapPost("/register", Placeholder);
        auth.MapPost("/login", Placeholder);

        var products = app.MapGroup("/api/products").WithTags("商品");
        products.MapGet("/", Placeholder);
        products.MapGet("/{id}", (string id) => Placeholder());
        products.MapPost("/", Placeholder);
        products.MapPut("/{id}/review", (string id) => Placeholder());

        app.MapGet("/api/categories", Placeholder).WithTags("商品");

        var orders = app.MapGroup("/api/orders").WithTags("购物车与订单");
        orders.MapGet("/cart", Placeholder);
        orders.MapPost("/cart", Placeholder);
        orders.MapPut("/cart/{cartItemId}", (string cartItemId) => Placeholder());
        orders.MapDelete("/cart/{cartItemId}", (string cartItemId) => Placeholder());
        orders.MapPost("/", Placeholder);
        orders.MapGet("/", Placeholder);
        orders.MapGet("/{id}", (string id) => Placeholder());
        orders.MapPost("/pay", Placeholder);
        orders.MapPut("/{id}/ship", (string id) => Placeholder());
        orders.MapPut("/{id}/complete", (string id) => Placeholder());
        orders.MapPut("/{id}/cancel", (string id) => Placeholder());

        var merchants = app.MapGroup("/api/merchants").WithTags("商家");
        merchants.MapPost("/apply", Placeholder);
        merchants.MapGet("/pending", Placeholder);
        merchants.MapPut("/{id}/approve", (string id) => Placeholder());
        merchants.MapGet("/my-products", Placeholder);
        merchants.MapGet("/my-orders", Placeholder);

        var reports = app.MapGroup("/api/reports").WithTags("管理与报表");
        reports.MapGet("/overview", Placeholder);
        reports.MapGet("/daily-sales", Placeholder);
        reports.MapGet("/category-sales", Placeholder);
        reports.MapGet("/merchant", Placeholder);

        var tickets = app.MapGroup("/api/tickets").WithTags("客服工单");
        tickets.MapPost("/", Placeholder);
        tickets.MapGet("/my", Placeholder);
        tickets.MapGet("/assigned", Placeholder);
        tickets.MapPut("/{id}/reply", (string id) => Placeholder());
        tickets.MapGet("/all", Placeholder);

        app.Run();
    }

    private static IResult Placeholder()
    {
        return Results.Ok(new ApiResponse<object>(
            true,
            "backend3: 当前接口测试通过",
            new { }));
    }

    private static void ValidateJwtOptions(JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer) ||
            string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("JWT issuer and audience must be configured.");
        }

        if (Encoding.UTF8.GetByteCount(options.SecretKey) < 32)
        {
            throw new InvalidOperationException("JWT secret must contain at least 32 bytes.");
        }

        if (options.ExpirationMinutes <= 0)
        {
            throw new InvalidOperationException("JWT expiration must be greater than zero.");
        }
    }
}

public sealed record ApiResponse<T>(bool Success, string Message, T Data);

