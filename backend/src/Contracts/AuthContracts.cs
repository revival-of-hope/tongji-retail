using RetailSystem.Api.Models;

namespace RetailSystem.Api.Contracts;

public sealed record RegisterRequest(
    string Username,
    string Password,
    string? Email,
    string? Phone);

public sealed record LoginRequest(string Username, string Password);

public sealed record UserSummary(
    long Id,
    string Username,
    string? Email,
    string? Phone,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt,
    MerchantSummary? Merchant);

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, UserSummary User);
