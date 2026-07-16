namespace RetailSystem.Api.Models;

public enum UserRole
{
    Admin = 0,
    Customer = 1,
    Merchant = 2,
    CustomerService = 3
}

public sealed class User
{
    public long Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public UserRole Role { get; set; } = UserRole.Customer;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Merchant? Merchant { get; set; }
    public ShoppingCart? ShoppingCart { get; set; }
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<ProductReview> Reviews { get; set; } = [];
    public ICollection<CustomerServiceTicket> SubmittedTickets { get; set; } = [];
    public ICollection<CustomerServiceTicket> AssignedTickets { get; set; } = [];
}
