using Microsoft.EntityFrameworkCore;
using RetailSystem.Api.Models;

namespace RetailSystem.Api.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");

        const int maxAttempts = 12;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await db.Database.EnsureCreatedAsync(cancellationToken);
                break;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "Database initialization attempt {Attempt}/{MaxAttempts} failed", attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        if (await db.Users.AnyAsync(cancellationToken)) return;

        var admin = NewUser("admin", "Admin123!", "admin@retail.local", UserRole.Admin);
        var customer = NewUser("customer", "Customer123!", "customer@retail.local", UserRole.Customer);
        var merchantUser = NewUser("merchant", "Merchant123!", "merchant@retail.local", UserRole.Merchant);
        var serviceUser = NewUser("service", "Service123!", "service@retail.local", UserRole.CustomerService);

        admin.ShoppingCart = new ShoppingCart();
        customer.ShoppingCart = new ShoppingCart();
        merchantUser.ShoppingCart = new ShoppingCart();
        serviceUser.ShoppingCart = new ShoppingCart();

        var merchant = new Merchant
        {
            User = merchantUser,
            StoreName = "同济优选店",
            Description = "课程项目演示商家",
            Status = MerchantStatus.Approved
        };

        var digital = new Category { Name = "数码", SortOrder = 1 };
        var home = new Category { Name = "家居", SortOrder = 2 };
        var books = new Category { Name = "图书", SortOrder = 3 };
        var food = new Category { Name = "食品", SortOrder = 4 };

        var products = new[]
        {
            NewProduct(merchant, digital, "无线机械键盘", "三模连接，热插拔轴体，适合办公与编程。", 399m, 80, "https://images.unsplash.com/photo-1587829741301-dc798b83add3?auto=format&fit=crop&w=1200&q=80"),
            NewProduct(merchant, digital, "降噪蓝牙耳机", "主动降噪与通透模式，续航 30 小时。", 699m, 45, "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?auto=format&fit=crop&w=1200&q=80"),
            NewProduct(merchant, home, "护眼阅读台灯", "无频闪，色温与亮度多档可调。", 189m, 120, "https://images.unsplash.com/photo-1507473885765-e6ed057f782c?auto=format&fit=crop&w=1200&q=80"),
            NewProduct(merchant, books, "计算机系统导论", "面向软件开发者的计算机系统基础读物。", 88m, 200, "https://images.unsplash.com/photo-1544947950-fa07a98d237f?auto=format&fit=crop&w=1200&q=80"),
            NewProduct(merchant, food, "精品挂耳咖啡", "十包装，中度烘焙，坚果与巧克力风味。", 59m, 300, "https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?auto=format&fit=crop&w=1200&q=80"),
            NewProduct(merchant, home, "人体工学坐垫", "高密度慢回弹材料，适合长时间学习。", 129m, 90, "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?auto=format&fit=crop&w=1200&q=80")
        };

        db.AddRange(admin, customer, merchantUser, serviceUser, merchant, digital, home, books, food);
        db.Products.AddRange(products);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded demo users and products");
    }

    private static User NewUser(string username, string password, string email, UserRole role) => new()
    {
        Username = username,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        Email = email,
        Role = role,
        IsActive = true
    };

    private static Product NewProduct(Merchant merchant, Category category, string name, string description, decimal price, int stock, string imageUrl) => new()
    {
        Merchant = merchant,
        Category = category,
        Name = name,
        Description = description,
        Price = price,
        StockQuantity = stock,
        Status = ProductStatus.OnSale,
        Images = [new ProductImage { ImageUrl = imageUrl, IsMain = true, SortOrder = 0 }]
    };
}
