using RetailSystem.Api.Services;

namespace RetailSystem.Api.Tests;

public sealed class ValidationTests
{
    [Fact]
    public void Register_Accepts_Valid_Input()
    {
        var error = Validation.Register(
            "testuser",
            "Password123!",
            "test@example.com",
            "13800138000");

        Assert.Null(error);
    }

    [Fact]
    public void Register_Rejects_Too_Short_Username()
    {
        var error = Validation.Register(
            "ab",
            "Password123!",
            null,
            null);

        Assert.NotNull(error);
    }

    [Fact]
    public void Register_Rejects_Invalid_Email()
    {
        var error = Validation.Register(
            "testuser",
            "Password123!",
            "not-an-email",
            null);

        Assert.NotNull(error);
    }

    [Fact]
    public void Login_Rejects_Empty_Username()
    {
        var error = Validation.Login(
            "   ",
            "Password123!");

        Assert.NotNull(error);
    }

    [Fact]
    public void Product_Accepts_Valid_Input()
    {
        var error = Validation.Product(
            "测试商品",
            "商品描述",
            100m,
            10,
            ["https://example.com/product.jpg"]);

        Assert.Null(error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Product_Rejects_NonPositive_Price(
        int price)
    {
        var error = Validation.Product(
            "测试商品",
            null,
            price,
            10,
            []);

        Assert.NotNull(error);
    }

    [Fact]
    public void Product_Rejects_Negative_Stock()
    {
        var error = Validation.Product(
            "测试商品",
            null,
            100m,
            -1,
            []);

        Assert.NotNull(error);
    }

    [Fact]
    public void Product_Rejects_More_Than_Eight_Images()
    {
        var images = Enumerable
            .Range(1, 9)
            .Select(index =>
                $"https://example.com/{index}.jpg")
            .ToArray();

        var error = Validation.Product(
            "测试商品",
            null,
            100m,
            10,
            images);

        Assert.NotNull(error);
    }

    [Fact]
    public void Product_Rejects_NonHttps_Image()
    {
        var error = Validation.Product(
            "测试商品",
            null,
            100m,
            10,
            ["http://example.com/product.jpg"]);

        Assert.NotNull(error);
    }

    [Fact]
    public void ProductQuery_Accepts_Valid_Query()
    {
        var error = Validation.ProductQuery(
            1,
            20,
            "键盘",
            100m,
            1000m,
            "newest");

        Assert.Null(error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000001)]
    public void ProductQuery_Rejects_Invalid_Page_Index(
        int pageIndex)
    {
        var error = Validation.ProductQuery(
            pageIndex,
            20,
            null,
            null,
            null,
            "newest");

        Assert.NotNull(error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void ProductQuery_Rejects_Invalid_Page_Size(
        int pageSize)
    {
        var error = Validation.ProductQuery(
            1,
            pageSize,
            null,
            null,
            null,
            "newest");

        Assert.NotNull(error);
    }

    [Fact]
    public void ProductQuery_Rejects_Reversed_Price_Range()
    {
        var error = Validation.ProductQuery(
            1,
            20,
            null,
            500m,
            100m,
            "newest");

        Assert.NotNull(error);
    }

    [Fact]
    public void ProductQuery_Rejects_Invalid_Sort_Option()
    {
        var error = Validation.ProductQuery(
            1,
            20,
            null,
            null,
            null,
            "unknown");

        Assert.NotNull(error);
    }

    [Fact]
    public void Ticket_Accepts_Valid_Input()
    {
        var error = Validation.Ticket(
            "订单问题",
            "我的订单出现了问题，请协助处理。");

        Assert.Null(error);
    }

    [Fact]
    public void Ticket_Rejects_Empty_Subject()
    {
        var error = Validation.Ticket(
            "   ",
            "工单内容");

        Assert.NotNull(error);
    }

    [Fact]
    public void Ticket_Rejects_Subject_Over_200_Characters()
    {
        var error = Validation.Ticket(
            new string('A', 201),
            "工单内容");

        Assert.NotNull(error);
    }

    [Fact]
    public void Ticket_Rejects_Description_Over_2000_Characters()
    {
        var error = Validation.Ticket(
            "测试工单",
            new string('A', 2001));

        Assert.NotNull(error);
    }

    [Fact]
    public void Boundary_Lengths_Are_Accepted()
    {
        var ticketError = Validation.Ticket(
            new string('A', 200),
            new string('B', 2000));

        var productError = Validation.Product(
            new string('A', 200),
            new string('B', Validation.MaxProductDescriptionLength),
            Validation.MaxMoney,
            0,
            []);

        Assert.Null(ticketError);
        Assert.Null(productError);
    }
}