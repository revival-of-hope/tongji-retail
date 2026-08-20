using System.Net;
using RetailSystem.Api.Contracts;
using RetailSystem.Api.Models;

namespace RetailSystem.Api.Tests;

public sealed class ReportTests
{
    [Fact]
    public async Task Anonymous_User_Cannot_Access_Reports()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var response =
            await client.GetAsync("/api/reports/overview");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Customer_Cannot_Access_Admin_Reports()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync(
            "customer",
            "Customer123!");

        var response =
            await client.GetAsync("/api/reports/overview");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Merchant_Cannot_Access_Admin_Reports()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync(
            "merchant",
            "Merchant123!");

        var response =
            await client.GetAsync("/api/reports/daily-sales");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Admin_Overview_Report_Reflects_Paid_Order()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var (_, paidOrder) =
            await CreatePaidOrderAsync(client, 2);

        await client.LoginAsync(
            "admin",
            "Admin123!");

        var response =
            await client.GetAsync("/api/reports/overview");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var report =
            await response.ReadDataAsync<OverviewReport>();

        Assert.Equal(
            paidOrder.TotalAmount,
            report.TotalSales);

        Assert.Equal(1, report.TotalOrders);

        Assert.Equal(4, report.TotalUsers);

        Assert.Equal(6, report.TotalProducts);

        Assert.Equal(0, report.PendingProducts);

        Assert.Equal(0, report.PendingMerchants);

        Assert.Equal(0, report.OpenTickets);
    }

    [Fact]
    public async Task Admin_Daily_Sales_Report_Reflects_Paid_Order()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var (_, paidOrder) =
            await CreatePaidOrderAsync(client, 2);

        Assert.NotNull(paidOrder.Payment);
        Assert.NotNull(paidOrder.Payment.PaidAt);

        await client.LoginAsync(
            "admin",
            "Admin123!");

        var response =
            await client.GetAsync("/api/reports/daily-sales");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var points =
            await response
                .ReadDataAsync<IReadOnlyList<DailySalesPoint>>();

        Assert.Equal(30, points.Count);

        for (var i = 1; i < points.Count; i++)
        {
            Assert.Equal(
                points[i - 1].Date.AddDays(1),
                points[i].Date);
        }

        var paidDate =
            DateOnly.FromDateTime(
                paidOrder.Payment.PaidAt.Value);

        var paidDay =
            Assert.Single(
                points,
                point => point.Date == paidDate);

        Assert.Equal(
            paidOrder.TotalAmount,
            paidDay.Sales);

        Assert.Equal(
            1,
            paidDay.Orders);
    }

    [Fact]
    public async Task Admin_Category_Sales_Report_Reflects_Paid_Order()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var (product, paidOrder) =
            await CreatePaidOrderAsync(client, 2);

        await client.LoginAsync(
            "admin",
            "Admin123!");

        var response =
            await client.GetAsync("/api/reports/category-sales");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var categories =
            await response
                .ReadDataAsync<IReadOnlyList<CategorySalesPoint>>();

        var category =
            Assert.Single(categories);

        Assert.Equal(
            product.CategoryName,
            category.CategoryName);

        Assert.Equal(
            paidOrder.TotalAmount,
            category.Sales);

        Assert.Equal(
            2,
            category.Quantity);

        Assert.True(
            category.CategoryId > 0);
    }

    [Fact]
    public async Task Merchant_Report_Reflects_Paid_Order()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var (product, paidOrder) =
            await CreatePaidOrderAsync(client, 2);

        Assert.NotNull(paidOrder.Payment);
        Assert.NotNull(paidOrder.Payment.PaidAt);

        await client.LoginAsync(
            "merchant",
            "Merchant123!");

        var response =
            await client.GetAsync("/api/reports/merchant");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var report =
            await response.ReadDataAsync<MerchantReport>();

        Assert.Equal(
            paidOrder.TotalAmount,
            report.TotalSales);

        Assert.Equal(
            1,
            report.TotalOrders);

        Assert.Equal(
            30,
            report.DailySales.Count);

        var topProduct =
            Assert.Single(report.TopProducts);

        Assert.Equal(
            product.Id,
            topProduct.ProductId);

        Assert.Equal(
            product.Name,
            topProduct.ProductName);

        Assert.Equal(
            2,
            topProduct.Quantity);

        Assert.Equal(
            paidOrder.TotalAmount,
            topProduct.Sales);

        var paidDate =
            DateOnly.FromDateTime(
                paidOrder.Payment.PaidAt.Value);

        var paidDay =
            Assert.Single(
                report.DailySales,
                point => point.Date == paidDate);

        Assert.Equal(
            paidOrder.TotalAmount,
            paidDay.Sales);

        Assert.Equal(
            1,
            paidDay.Orders);
    }

    [Fact]
    public async Task Customer_Cannot_Access_Merchant_Report()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync(
            "customer",
            "Customer123!");

        var response =
            await client.GetAsync("/api/reports/merchant");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    private static async Task<(
        ProductListItem Product,
        OrderDetail PaidOrder)> CreatePaidOrderAsync(
        HttpClient client,
        int quantity)
    {
        await client.LoginAsync(
            "customer",
            "Customer123!");

        var productsResponse =
            await client.GetAsync(
                "/api/products/?pageSize=1");

        Assert.Equal(
            HttpStatusCode.OK,
            productsResponse.StatusCode);

        var products =
            await productsResponse
                .ReadDataAsync<PagedResponse<ProductListItem>>();

        var product =
            Assert.Single(products.Items);

        var addResponse =
            await client.PostAsApiJsonAsync(
                "/api/cart/items",
                new AddCartItemRequest(
                    product.Id,
                    quantity));

        Assert.Equal(
            HttpStatusCode.Created,
            addResponse.StatusCode);

        var cartItem =
            await addResponse
                .ReadDataAsync<CartItemResponse>();

        var createResponse =
            await client.PostAsApiJsonAsync(
                "/api/orders/",
                new CreateOrderRequest(
                    [cartItem.CartItemId],
                    "上海市杨浦区四平路1239号",
                    "report integration test"));

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var order =
            await createResponse
                .ReadDataAsync<OrderDetail>();

        Assert.Equal(
            OrderStatus.PendingPayment,
            order.Status);

        var payResponse =
            await client.PostAsApiJsonAsync(
                $"/api/orders/{order.Id}/pay",
                new PayOrderRequest(
                    PaymentMethod.Alipay));

        Assert.Equal(
            HttpStatusCode.OK,
            payResponse.StatusCode);

        var paidOrder =
            await payResponse
                .ReadDataAsync<OrderDetail>();

        Assert.Equal(
            OrderStatus.PendingShipment,
            paidOrder.Status);

        Assert.NotNull(
            paidOrder.Payment);

        Assert.Equal(
            PaymentStatus.Success,
            paidOrder.Payment.Status);

        Assert.Equal(
            product.Price * quantity,
            paidOrder.TotalAmount);

        return (
            product,
            paidOrder);
    }
}