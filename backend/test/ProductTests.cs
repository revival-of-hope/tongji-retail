using System.Net;
using System.Net.Http.Json;
using RetailSystem.Api.Contracts;
using RetailSystem.Api.Models;

namespace RetailSystem.Api.Tests;

public sealed class ProductTests
{
    [Fact]
    public async Task Product_List_Uses_Seeded_Oracle_Compatible_Model()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products/?pageIndex=1&pageSize=10&sortBy=newest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.ReadDataAsync<PagedResponse<ProductListItem>>();
        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, product => Assert.Equal(ProductStatus.OnSale, product.Status));
    }

    [Fact]
    public async Task Customer_Cannot_Create_Product()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();
        await client.LoginAsync("customer", "Customer123!");

        var response = await client.PostAsJsonAsync("/api/products/", new CreateProductRequest(
            1, "Unauthorized product", null, 1m, 1, null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Merchant_Edit_Reenters_Review_And_Remains_Visible_To_Owner()
    {
        await using var factory = new RetailApiFactory();
        using var merchantClient = factory.CreateClient();
        await merchantClient.LoginAsync("merchant", "Merchant123!");

        var listResponse = await merchantClient.GetAsync("/api/merchants/my-products");
        listResponse.EnsureSuccessStatusCode();
        var products = await listResponse.ReadDataAsync<IReadOnlyList<ProductListItem>>();
        Assert.NotEmpty(products);
        var product = products[0];

        var detailResponse = await merchantClient.GetAsync($"/api/products/{product.Id}");
        var detail = await detailResponse.ReadDataAsync<ProductDetail>();
        var update = await merchantClient.PutAsJsonAsync($"/api/products/{product.Id}", new UpdateProductRequest(
            detail.CategoryId,
            detail.Name + "（更新）",
            detail.Description,
            detail.Price,
            detail.StockQuantity,
            detail.ImageUrls));

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.ReadDataAsync<ProductDetail>();
        Assert.Equal(ProductStatus.PendingReview, updated.Status);

        var ownerView = await merchantClient.GetAsync($"/api/products/{product.Id}");
        Assert.Equal(HttpStatusCode.OK, ownerView.StatusCode);

        using var anonymousClient = factory.CreateClient();
        var anonymousView = await anonymousClient.GetAsync($"/api/products/{product.Id}");
        Assert.Equal(HttpStatusCode.NotFound, anonymousView.StatusCode);
    }
}
