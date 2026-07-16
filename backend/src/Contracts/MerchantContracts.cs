using RetailSystem.Api.Models;

namespace RetailSystem.Api.Contracts;

public sealed record ApplyMerchantRequest(string StoreName, string? Description);

public sealed record MerchantSummary(
    long Id,
    long UserId,
    string StoreName,
    string? Description,
    MerchantStatus Status,
    DateTime CreatedAt);

public sealed record ReviewMerchantRequest(bool Approved);
