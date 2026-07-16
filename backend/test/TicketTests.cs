using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RetailSystem.Api.Contracts;
using RetailSystem.Api.Data;
using RetailSystem.Api.Models;

namespace RetailSystem.Api.Tests;

public sealed class TicketTests
{
    [Fact]
    public async Task Customer_Submitted_Ticket_Is_Visible_To_Assigned_Service_User()
    {
        await using var factory = new RetailApiFactory();

        using var customerClient = factory.CreateClient();
        await customerClient.LoginAsync("customer", "Customer123!");
        var createResponse = await customerClient.PostAsJsonAsync(
            "/api/tickets/",
            new CreateTicketRequest(null, "测试工单", "需要客服协助处理"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.ReadDataAsync<TicketResponse>();
        Assert.NotNull(created.AssignedTo);

        using var serviceClient = factory.CreateClient();
        await serviceClient.LoginAsync("service", "Service123!");
        var assignedResponse = await serviceClient.GetAsync("/api/tickets/assigned");

        Assert.Equal(HttpStatusCode.OK, assignedResponse.StatusCode);
        var tickets = await assignedResponse.ReadDataAsync<IReadOnlyList<TicketResponse>>();
        Assert.Contains(tickets, ticket => ticket.Id == created.Id);
    }

    [Fact]
    public async Task Unassigned_Ticket_Is_Visible_To_Service_And_Is_Claimed_On_Reply()
    {
        await using var factory = new RetailApiFactory();
        long ticketId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var customerId = await db.Users
                .Where(user => user.Username == "customer")
                .Select(user => user.Id)
                .SingleAsync();

            var ticket = new CustomerServiceTicket
            {
                UserId = customerId,
                Subject = "未分配工单",
                Description = "等待客服接单",
                Status = TicketStatus.Pending,
                AssignedTo = null
            };
            db.CustomerServiceTickets.Add(ticket);
            await db.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        using var serviceClient = factory.CreateClient();
        var service = await serviceClient.LoginAsync("service", "Service123!");
        var listResponse = await serviceClient.GetAsync("/api/tickets/assigned");
        var tickets = await listResponse.ReadDataAsync<IReadOnlyList<TicketResponse>>();
        Assert.Contains(tickets, ticket => ticket.Id == ticketId && ticket.AssignedTo is null);

        var replyResponse = await serviceClient.PutAsJsonAsync(
            $"/api/tickets/{ticketId}/reply",
            new ReplyTicketRequest("已接单处理", TicketStatus.Processing));

        Assert.Equal(HttpStatusCode.OK, replyResponse.StatusCode);
        var updated = await replyResponse.ReadDataAsync<TicketResponse>();
        Assert.Equal(service.User.Id, updated.AssignedTo);
    }
}
