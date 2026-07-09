namespace Api;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

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

        app.MapGet("/", () => Results.Ok(new ApiResponse<object>(
            true,
            "backend3: 后端服务运行中",
            new { })));

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
}

public sealed record ApiResponse<T>(bool Success, string Message, T Data);

