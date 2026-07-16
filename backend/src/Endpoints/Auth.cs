using Microsoft.EntityFrameworkCore;
using RetailSystem.Api.Contracts;
using RetailSystem.Api.Data;
using RetailSystem.Api.Models;
using RetailSystem.Api.Services;

namespace RetailSystem.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .WithSummary("注册顾客账号")
            .Produces<ApiEnvelope<AuthResponse>>(StatusCodes.Status201Created)
            .WithStandardErrors();

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("用户名和密码登录")
            .Produces<ApiEnvelope<AuthResponse>>()
            .WithStandardErrors();

        group.MapGet("/me", MeAsync)
            .WithName("GetCurrentUser")
            .WithSummary("获取当前登录用户")
            .RequireAuthorization()
            .Produces<ApiEnvelope<UserSummary>>()
            .WithStandardErrors();

        return app;
    }

    private static async Task<IResult> RegisterAsync(RegisterRequest request, AppDbContext db, JwtService jwt, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant();
        var error = Validation.Register(username, request.Password, email);
        if (error is not null) return ApiResults.BadRequest(error);

        if (await db.Users.AnyAsync(x => x.Username == username, cancellationToken))
            return ApiResults.Conflict("用户名已存在");
        if (email is not null && await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
            return ApiResults.Conflict("邮箱已被使用");

        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Email = email,
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Role = UserRole.Customer,
            ShoppingCart = new ShoppingCart()
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        var token = jwt.Generate(user);
        var response = new AuthResponse(token.Value, token.ExpiresAt, user.ToSummary());
        return ApiResults.Created("/api/auth/me", response, "注册成功");
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, AppDbContext db, JwtService jwt, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var user = await db.Users.Include(x => x.Merchant).SingleOrDefaultAsync(x => x.Username == username, cancellationToken);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return ApiResults.Unauthorized("用户名或密码错误");
        if (!user.IsActive) return ApiResults.Forbidden("账号已被停用");

        var token = jwt.Generate(user);
        return ApiResults.Ok(new AuthResponse(token.Value, token.ExpiresAt, user.ToSummary()), "登录成功");
    }

    private static async Task<IResult> MeAsync(System.Security.Claims.ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var user = await db.Users.Include(x => x.Merchant).SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        return user is null ? ApiResults.NotFound("用户不存在") : ApiResults.Ok(user.ToSummary());
    }
}
