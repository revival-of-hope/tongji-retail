using System.Net;
using RetailSystem.Api.Contracts;
using RetailSystem.Api.Models;

namespace RetailSystem.Api.Tests;

public sealed class TicketReplyValidationTests
{
    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public async Task Reply_With_Invalid_Ticket_Id_Returns_BadRequest(
        long ticketId)
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("service", "Service123!");

        var response = await client.PutAsApiJsonAsync(
            $"/api/tickets/{ticketId}/reply",
            new ReplyTicketRequest(
                "正在处理该问题",
                TicketStatus.Processing));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Empty_Reply_Returns_BadRequest()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("service", "Service123!");

        var response = await client.PutAsApiJsonAsync(
            "/api/tickets/1/reply",
            new ReplyTicketRequest(
                "   ",
                TicketStatus.Processing));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Reply_Over_2000_Characters_Returns_BadRequest()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("service", "Service123!");

        var response = await client.PutAsApiJsonAsync(
            "/api/tickets/1/reply",
            new ReplyTicketRequest(
                new string('A', 2001),
                TicketStatus.Processing));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Reply_Cannot_Set_Status_Back_To_Pending()
    {
        await using var factory = new RetailApiFactory();
        using var client = factory.CreateClient();

        await client.LoginAsync("service", "Service123!");

        var response = await client.PutAsApiJsonAsync(
            "/api/tickets/1/reply",
            new ReplyTicketRequest(
                "已经回复用户",
                TicketStatus.Pending));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
}