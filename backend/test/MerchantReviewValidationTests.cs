using System.Net;
using RetailSystem.Api.Contracts;
using RetailSystem.Api.Models;

namespace RetailSystem.Api.Tests;

public sealed class MerchantReviewValidationTests
{
    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public async Task Review_With_Invalid_Merchant_Id_Returns_BadRequest(
        long merchantId)
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("admin", "Admin123!");

        var response = await client.PutAsApiJsonAsync(
            $"/api/merchants/{merchantId}/review",
            new ReviewMerchantRequest(true));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Reviewing_The_Same_Application_Twice_Returns_Conflict()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("customer", "Customer123!");

        var applyResponse = await client.PostAsApiJsonAsync(
            "/api/merchants/apply",
            new ApplyMerchantRequest(
                "重复审核测试店铺",
                "用于测试重复审核"));

        Assert.Equal(
            HttpStatusCode.Created,
            applyResponse.StatusCode);

        var merchant =
            await applyResponse.ReadDataAsync<MerchantSummary>();

        await client.LoginAsync("admin", "Admin123!");

        var firstReview = await client.PutAsApiJsonAsync(
            $"/api/merchants/{merchant.Id}/review",
            new ReviewMerchantRequest(true));

        Assert.Equal(
            HttpStatusCode.OK,
            firstReview.StatusCode);

        var secondReview = await client.PutAsApiJsonAsync(
            $"/api/merchants/{merchant.Id}/review",
            new ReviewMerchantRequest(false));

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondReview.StatusCode);
    }

    [Fact]
    public async Task Admin_Can_Reject_A_Pending_Application()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("customer", "Customer123!");

        var applyResponse = await client.PostAsApiJsonAsync(
            "/api/merchants/apply",
            new ApplyMerchantRequest(
                "拒绝审核测试店铺",
                "该申请用于测试拒绝流程"));

        var application =
            await applyResponse.ReadDataAsync<MerchantSummary>();

        await client.LoginAsync("admin", "Admin123!");

        var reviewResponse = await client.PutAsApiJsonAsync(
            $"/api/merchants/{application.Id}/review",
            new ReviewMerchantRequest(false));

        Assert.Equal(
            HttpStatusCode.OK,
            reviewResponse.StatusCode);

        var reviewed =
            await reviewResponse.ReadDataAsync<MerchantSummary>();

        Assert.Equal(
            MerchantStatus.Rejected,
            reviewed.Status);

        var customerAuth =
            await client.LoginAsync("customer", "Customer123!");

        Assert.Equal(
            UserRole.Customer,
            customerAuth.User.Role);
    }
}