using System.Net;
using RetailSystem.Api.Contracts;
using RetailSystem.Api.Models;

namespace RetailSystem.Api.Tests;

public sealed class MerchantTests
{
    [Fact]
    public async Task Customer_Can_Apply_And_Admin_Can_Approve_Merchant()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        // 普通顾客登录
        await client.LoginAsync("customer", "Customer123!");

        // 提交商家申请
        var applyResponse = await client.PostAsApiJsonAsync(
            "/api/merchants/apply",
            new ApplyMerchantRequest(
                "Backend01 Test Store",
                "Merchant integration test"));

        Assert.Equal(HttpStatusCode.Created, applyResponse.StatusCode);

        var application =
            await applyResponse.ReadDataAsync<MerchantSummary>();

        Assert.Equal("Backend01 Test Store", application.StoreName);
        Assert.Equal(
            "Merchant integration test",
            application.Description);
        Assert.Equal(MerchantStatus.Pending, application.Status);

        // 切换成管理员
        await client.LoginAsync("admin", "Admin123!");

        // 管理员查看待审核商家
        var pendingResponse =
            await client.GetAsync("/api/merchants/pending");

        Assert.Equal(HttpStatusCode.OK, pendingResponse.StatusCode);

        var pending =
            await pendingResponse
                .ReadDataAsync<IReadOnlyList<MerchantSummary>>();

        Assert.Contains(
            pending,
            merchant => merchant.Id == application.Id);

        // 管理员批准申请
        var reviewResponse = await client.PutAsApiJsonAsync(
            $"/api/merchants/{application.Id}/review",
            new ReviewMerchantRequest(true));

        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);

        var reviewed =
            await reviewResponse.ReadDataAsync<MerchantSummary>();

        Assert.Equal(application.Id, reviewed.Id);
        Assert.Equal(MerchantStatus.Approved, reviewed.Status);

        // 原 customer 用户重新登录
        // 审核通过以后角色应该已经变成 Merchant
        var newAuth =
            await client.LoginAsync("customer", "Customer123!");

        Assert.Equal(UserRole.Merchant, newAuth.User.Role);

        // 获取自己的商家信息
        var mineResponse =
            await client.GetAsync("/api/merchants/me");

        Assert.Equal(HttpStatusCode.OK, mineResponse.StatusCode);

        var mine =
            await mineResponse.ReadDataAsync<MerchantSummary>();

        Assert.Equal(application.Id, mine.Id);
        Assert.Equal("Backend01 Test Store", mine.StoreName);
        Assert.Equal(MerchantStatus.Approved, mine.Status);
    }

    [Fact]
    public async Task Empty_Store_Name_Returns_BadRequest()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("customer", "Customer123!");

        var response = await client.PostAsApiJsonAsync(
            "/api/merchants/apply",
            new ApplyMerchantRequest(
                "   ",
                "Invalid store name"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Duplicate_Merchant_Application_Returns_Conflict()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("customer", "Customer123!");

        // 第一次申请
        var firstResponse = await client.PostAsApiJsonAsync(
            "/api/merchants/apply",
            new ApplyMerchantRequest(
                "First Test Store",
                "First application"));

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        // 同一个用户再次申请
        var secondResponse = await client.PostAsApiJsonAsync(
            "/api/merchants/apply",
            new ApplyMerchantRequest(
                "Second Test Store",
                "Duplicate application"));

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task Customer_Cannot_Get_Pending_Merchants()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("customer", "Customer123!");

        var response =
            await client.GetAsync("/api/merchants/pending");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Merchant_Cannot_Submit_Customer_Merchant_Application()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("merchant", "Merchant123!");

        var response = await client.PostAsApiJsonAsync(
            "/api/merchants/apply",
            new ApplyMerchantRequest(
                "Another Store",
                "Merchant should not apply again"));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Admin_Can_Get_Pending_Merchant_List()
    {
        await using var factory = new RetailApiFactory();

        using var customerClient = factory.CreateClient();
        using var adminClient = factory.CreateClient();

        // 先制造一个待审核申请
        await customerClient.LoginAsync(
            "customer",
            "Customer123!");

        var applyResponse =
            await customerClient.PostAsApiJsonAsync(
                "/api/merchants/apply",
                new ApplyMerchantRequest(
                    "Pending Test Store",
                    "Waiting for review"));

        Assert.Equal(
            HttpStatusCode.Created,
            applyResponse.StatusCode);

        var application =
            await applyResponse.ReadDataAsync<MerchantSummary>();

        // 管理员查询
        await adminClient.LoginAsync(
            "admin",
            "Admin123!");

        var response =
            await adminClient.GetAsync("/api/merchants/pending");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var merchants =
            await response
                .ReadDataAsync<IReadOnlyList<MerchantSummary>>();

        Assert.Contains(
            merchants,
            merchant =>
                merchant.Id == application.Id &&
                merchant.Status == MerchantStatus.Pending);
    }
}