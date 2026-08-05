using Microsoft.EntityFrameworkCore;
using RetailSystem.Api.Models;

namespace RetailSystem.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ShoppingCart> ShoppingCarts => Set<ShoppingCart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<CustomerServiceTicket> CustomerServiceTickets => Set<CustomerServiceTicket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUsers(modelBuilder);
        ConfigureMerchants(modelBuilder);
        ConfigureCategories(modelBuilder);
        ConfigureProducts(modelBuilder);
        ConfigureCarts(modelBuilder);
        ConfigureOrders(modelBuilder);
        ConfigureReviewsAndTickets(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<User>();
        entity.ToTable("USERS", table =>
        {
            table.HasCheckConstraint("CK_USERS_ROLE", "\"Role\" BETWEEN 0 AND 3");
            table.HasCheckConstraint("CK_USERS_ACTIVE", "\"IsActive\" IN (0, 1)");
        });
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).ValueGeneratedOnAdd();
        entity.Property(x => x.Username).HasMaxLength(50).IsRequired();
        entity.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
        entity.Property(x => x.Email).HasMaxLength(100);
        entity.Property(x => x.Phone).HasMaxLength(20);
        entity.Property(x => x.Role).HasConversion<int>().HasColumnType("NUMBER(10)");
        entity.Property(x => x.IsActive).HasColumnType("NUMBER(1)");
        entity.HasIndex(x => x.Username).IsUnique();
        entity.HasIndex(x => x.Email).IsUnique();
        entity.HasIndex(x => new { x.Role, x.IsActive });
    }

    private static void ConfigureMerchants(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Merchant>();
        entity.ToTable("MERCHANTS", table =>
            table.HasCheckConstraint("CK_MERCHANTS_STATUS", "\"Status\" BETWEEN 0 AND 2"));
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).ValueGeneratedOnAdd();
        entity.Property(x => x.StoreName).HasMaxLength(100).IsRequired();
        entity.Property(x => x.Description).HasMaxLength(500);
        entity.Property(x => x.Status).HasConversion<int>().HasColumnType("NUMBER(10)");
        entity.HasIndex(x => x.UserId).IsUnique();
        entity.HasIndex(x => new { x.Status, x.CreatedAt });
        entity.HasOne(x => x.User)
            .WithOne(x => x.Merchant)
            .HasForeignKey<Merchant>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureCategories(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Category>();
        entity.ToTable("CATEGORIES");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).ValueGeneratedOnAdd();
        entity.Property(x => x.Name).HasMaxLength(50).IsRequired();
        entity.HasIndex(x => new { x.ParentId, x.Name }).IsUnique();
        entity.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureProducts(ModelBuilder modelBuilder)
    {
        var product = modelBuilder.Entity<Product>();
        product.ToTable("PRODUCTS", table =>
        {
            table.HasCheckConstraint("CK_PRODUCTS_PRICE", "\"Price\" > 0");
            table.HasCheckConstraint("CK_PRODUCTS_STOCK", "\"StockQuantity\" >= 0");
            table.HasCheckConstraint("CK_PRODUCTS_SOLD", "\"SoldCount\" >= 0");
            table.HasCheckConstraint("CK_PRODUCTS_RATING", "\"AvgRating\" BETWEEN 0 AND 5");
            table.HasCheckConstraint("CK_PRODUCTS_REVIEW_COUNT", "\"ReviewCount\" >= 0");
            table.HasCheckConstraint("CK_PRODUCTS_STATUS", "\"Status\" BETWEEN 0 AND 3");
        });
        product.HasKey(x => x.Id);
        product.Property(x => x.Id).ValueGeneratedOnAdd();
        product.Property(x => x.Name).HasMaxLength(200).IsRequired();
        product.Property(x => x.Description).HasColumnType("CLOB");
        product.Property(x => x.Price).HasColumnType("NUMBER(18,2)");
        product.Property(x => x.AvgRating).HasColumnType("NUMBER(3,2)");
        product.Property(x => x.Status).HasConversion<int>().HasColumnType("NUMBER(10)");
        product.HasIndex(x => new { x.Status, x.CategoryId });
        product.HasIndex(x => new { x.MerchantId, x.Status });
        product.HasOne(x => x.Merchant)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.MerchantId)
            .OnDelete(DeleteBehavior.Restrict);
        product.HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        var image = modelBuilder.Entity<ProductImage>();
        image.ToTable("PRODUCT_IMAGES", table =>
        {
            table.HasCheckConstraint("CK_PRODUCT_IMAGES_MAIN", "\"IsMain\" IN (0, 1)");
            table.HasCheckConstraint("CK_PRODUCT_IMAGES_SORT", "\"SortOrder\" >= 0");
        });
        image.HasKey(x => x.Id);
        image.Property(x => x.Id).ValueGeneratedOnAdd();
        image.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
        image.Property(x => x.IsMain).HasColumnType("NUMBER(1)");
        image.HasIndex(x => new { x.ProductId, x.SortOrder });
        image.HasOne(x => x.Product)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureCarts(ModelBuilder modelBuilder)
    {
        var cart = modelBuilder.Entity<ShoppingCart>();
        cart.ToTable("SHOPPING_CARTS");
        cart.HasKey(x => x.Id);
        cart.Property(x => x.Id).ValueGeneratedOnAdd();
        cart.HasIndex(x => x.UserId).IsUnique();
        cart.HasOne(x => x.User)
            .WithOne(x => x.ShoppingCart)
            .HasForeignKey<ShoppingCart>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        var item = modelBuilder.Entity<CartItem>();
        item.ToTable("CART_ITEMS", table =>
            table.HasCheckConstraint("CK_CART_ITEMS_QUANTITY", "\"Quantity\" > 0"));
        item.HasKey(x => x.Id);
        item.Property(x => x.Id).ValueGeneratedOnAdd();
        item.HasIndex(x => new { x.CartId, x.ProductId }).IsUnique();
        item.HasOne(x => x.Cart)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.CartId)
            .OnDelete(DeleteBehavior.Cascade);
        item.HasOne(x => x.Product)
            .WithMany(x => x.CartItems)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureOrders(ModelBuilder modelBuilder)
    {
        var order = modelBuilder.Entity<Order>();
        order.ToTable("ORDERS", table =>
        {
            table.HasCheckConstraint("CK_ORDERS_TOTAL", "\"TotalAmount\" >= 0");
            table.HasCheckConstraint("CK_ORDERS_STATUS", "\"Status\" BETWEEN 0 AND 4");
        });
        order.HasKey(x => x.Id);
        order.Property(x => x.Id).ValueGeneratedOnAdd();
        order.Property(x => x.OrderNo).HasMaxLength(50).IsRequired();
        order.Property(x => x.TotalAmount).HasColumnType("NUMBER(18,2)");
        order.Property(x => x.Status).HasConversion<int>().HasColumnType("NUMBER(10)");
        order.Property(x => x.ShippingAddress).HasMaxLength(500).IsRequired();
        order.Property(x => x.Remark).HasMaxLength(500);
        order.HasIndex(x => x.OrderNo).IsUnique();
        order.HasIndex(x => new { x.UserId, x.Status, x.CreatedAt });
        order.HasIndex(x => new { x.Status, x.ExpireAt });
        order.HasOne(x => x.User)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        var item = modelBuilder.Entity<OrderItem>();
        item.ToTable("ORDER_ITEMS", table =>
        {
            table.HasCheckConstraint("CK_ORDER_ITEMS_QUANTITY", "\"Quantity\" > 0");
            table.HasCheckConstraint("CK_ORDER_ITEMS_UNIT_PRICE", "\"UnitPrice\" > 0");
            table.HasCheckConstraint("CK_ORDER_ITEMS_SUBTOTAL", "\"SubTotal\" > 0");
        });
        item.HasKey(x => x.Id);
        item.Property(x => x.Id).ValueGeneratedOnAdd();
        item.Property(x => x.UnitPrice).HasColumnType("NUMBER(18,2)");
        item.Property(x => x.SubTotal).HasColumnType("NUMBER(18,2)");
        item.HasIndex(x => new { x.OrderId, x.ProductId });
        item.HasIndex(x => x.ProductId);
        item.HasOne(x => x.Order)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        item.HasOne(x => x.Product)
            .WithMany(x => x.OrderItems)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        var payment = modelBuilder.Entity<Payment>();
        payment.ToTable("PAYMENTS", table =>
        {
            table.HasCheckConstraint("CK_PAYMENTS_AMOUNT", "\"Amount\" > 0");
            table.HasCheckConstraint("CK_PAYMENTS_METHOD", "\"PaymentMethod\" BETWEEN 0 AND 2");
            table.HasCheckConstraint("CK_PAYMENTS_STATUS", "\"Status\" BETWEEN 0 AND 3");
        });
        payment.HasKey(x => x.Id);
        payment.Property(x => x.Id).ValueGeneratedOnAdd();
        payment.Property(x => x.Amount).HasColumnType("NUMBER(18,2)");
        payment.Property(x => x.PaymentMethod).HasConversion<int>().HasColumnType("NUMBER(10)");
        payment.Property(x => x.Status).HasConversion<int>().HasColumnType("NUMBER(10)");
        payment.Property(x => x.TransactionId).HasMaxLength(100);
        payment.HasIndex(x => x.OrderId).IsUnique();
        payment.HasIndex(x => new { x.Status, x.PaidAt });
        payment.HasOne(x => x.Order)
            .WithOne(x => x.Payment)
            .HasForeignKey<Payment>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureReviewsAndTickets(ModelBuilder modelBuilder)
    {
        var review = modelBuilder.Entity<ProductReview>();
        review.ToTable("PRODUCT_REVIEWS", table =>
            table.HasCheckConstraint("CK_PRODUCT_REVIEWS_RATING", "\"Rating\" BETWEEN 1 AND 5"));
        review.HasKey(x => x.Id);
        review.Property(x => x.Id).ValueGeneratedOnAdd();
        review.Property(x => x.Comment).HasMaxLength(1000);
        review.HasIndex(x => new { x.OrderId, x.ProductId }).IsUnique();
        review.HasIndex(x => new { x.ProductId, x.CreatedAt });
        review.HasOne(x => x.Product)
            .WithMany(x => x.Reviews)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        review.HasOne(x => x.User)
            .WithMany(x => x.Reviews)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        review.HasOne(x => x.Order)
            .WithMany(x => x.Reviews)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        var ticket = modelBuilder.Entity<CustomerServiceTicket>();
        ticket.ToTable("CUSTOMER_SERVICE_TICKETS", table =>
            table.HasCheckConstraint("CK_TICKETS_STATUS", "\"Status\" BETWEEN 0 AND 3"));
        ticket.HasKey(x => x.Id);
        ticket.Property(x => x.Id).ValueGeneratedOnAdd();
        ticket.Property(x => x.Subject).HasMaxLength(200).IsRequired();
        ticket.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        ticket.Property(x => x.Reply).HasMaxLength(2000);
        ticket.Property(x => x.Status).HasConversion<int>().HasColumnType("NUMBER(10)");
        ticket.HasIndex(x => new { x.AssignedTo, x.Status, x.UpdatedAt });
        ticket.HasIndex(x => new { x.UserId, x.CreatedAt });
        ticket.HasOne(x => x.User)
            .WithMany(x => x.SubmittedTickets)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        ticket.HasOne(x => x.Order)
            .WithMany(x => x.Tickets)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        ticket.HasOne(x => x.AssignedUser)
            .WithMany(x => x.AssignedTickets)
            .HasForeignKey(x => x.AssignedTo)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
