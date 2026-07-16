using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RetailSystem.Api.Contracts;
using RetailSystem.Api.Data;
using RetailSystem.Api.Models;
using RetailSystem.Api.Services;

namespace RetailSystem.Api.Endpoints;

public static class TicketEndpoints
{
    public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tickets").WithTags("Tickets").RequireAuthorization();

        group.MapPost("/", CreateAsync)
            .WithName("CreateTicket")
            .WithSummary("顾客提交客服工单")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Customer)))
            .Produces<ApiEnvelope<TicketResponse>>(StatusCodes.Status201Created)
            .WithStandardErrors();

        group.MapGet("/mine", GetMineAsync)
            .WithName("GetMyTickets")
            .WithSummary("顾客获取自己提交的工单")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.Customer)))
            .Produces<ApiEnvelope<IReadOnlyList<TicketResponse>>>()
            .WithStandardErrors();

        group.MapGet("/assigned", GetAssignedAsync)
            .WithName("GetAssignedTickets")
            .WithSummary("客服获取分配给自己或尚未分配的工单")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.CustomerService)))
            .Produces<ApiEnvelope<IReadOnlyList<TicketResponse>>>()
            .WithStandardErrors();

        group.MapPut("/{id:long}/reply", ReplyAsync)
            .WithName("ReplyTicket")
            .WithSummary("客服或管理员回复并更新工单状态")
            .RequireAuthorization(policy => policy.RequireRole(nameof(UserRole.CustomerService), nameof(UserRole.Admin)))
            .Produces<ApiEnvelope<TicketResponse>>()
            .WithStandardErrors();

        return app;
    }

    private static async Task<IResult> CreateAsync(CreateTicketRequest request, ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        var error = Validation.Ticket(request.Subject, request.Description);
        if (error is not null) return ApiResults.BadRequest(error);
        var userId = principal.GetUserId();
        if (request.OrderId.HasValue && !await db.Orders.AnyAsync(x => x.Id == request.OrderId && x.UserId == userId, cancellationToken))
            return ApiResults.BadRequest("关联订单不存在或不属于当前用户");

        var serviceUserIds = await db.Users
            .Where(x => x.Role == UserRole.CustomerService && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        long? assignedTo = null;
        if (serviceUserIds.Count > 0)
        {
            var activeTicketCounts = await db.CustomerServiceTickets
                .Where(x => x.AssignedTo.HasValue
                    && serviceUserIds.Contains(x.AssignedTo.Value)
                    && x.Status != TicketStatus.Closed
                    && x.Status != TicketStatus.Resolved)
                .GroupBy(x => x.AssignedTo!.Value)
                .Select(group => new { UserId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

            assignedTo = serviceUserIds
                .OrderBy(id => activeTicketCounts.GetValueOrDefault(id))
                .ThenBy(id => id)
                .First();
        }

        var ticket = new CustomerServiceTicket
        {
            UserId = userId,
            OrderId = request.OrderId,
            AssignedTo = assignedTo,
            Subject = request.Subject.Trim(),
            Description = request.Description.Trim(),
            Status = assignedTo.HasValue ? TicketStatus.Processing : TicketStatus.Pending
        };
        db.CustomerServiceTickets.Add(ticket);
        await db.SaveChangesAsync(cancellationToken);
        ticket = await TicketQuery(db).SingleAsync(x => x.Id == ticket.Id, cancellationToken);
        return ApiResults.Created($"/api/tickets/{ticket.Id}", ticket.ToResponse(), "工单已提交");
    }

    private static async Task<IResult> GetMineAsync(ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        var tickets = await TicketQuery(db).AsNoTracking().Where(x => x.UserId == principal.GetUserId())
            .OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return ApiResults.Ok<IReadOnlyList<TicketResponse>>(tickets.Select(x => x.ToResponse()).ToArray());
    }

    private static async Task<IResult> GetAssignedAsync(ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        var tickets = await TicketQuery(db).AsNoTracking().Where(x => x.AssignedTo == userId || x.AssignedTo == null)
            .OrderBy(x => x.Status).ThenByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return ApiResults.Ok<IReadOnlyList<TicketResponse>>(tickets.Select(x => x.ToResponse()).ToArray());
    }

    private static async Task<IResult> ReplyAsync(long id, ReplyTicketRequest request, ClaimsPrincipal principal, AppDbContext db, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reply) || request.Reply.Trim().Length > 2000)
            return ApiResults.BadRequest("回复不能为空且不能超过 2000 个字符");
        if (request.Status is TicketStatus.Pending) return ApiResults.BadRequest("回复后状态不能设置为待处理");

        var ticket = await TicketQuery(db).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (ticket is null) return ApiResults.NotFound("工单不存在");
        var userId = principal.GetUserId();
        if (principal.GetRoleName() == nameof(UserRole.CustomerService)
            && ticket.AssignedTo.HasValue
            && ticket.AssignedTo.Value != userId)
            return ApiResults.Forbidden("只能处理分配给自己或尚未分配的工单");

        ticket.AssignedTo ??= userId;
        ticket.Reply = request.Reply.Trim();
        ticket.Status = request.Status;
        ticket.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ApiResults.Ok(ticket.ToResponse(), "工单已更新");
    }

    internal static IQueryable<CustomerServiceTicket> TicketQuery(AppDbContext db) => db.CustomerServiceTickets
        .Include(x => x.User)
        .Include(x => x.AssignedUser);
}
