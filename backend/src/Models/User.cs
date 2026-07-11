#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RetailSystem.Backend.Models;

// ============================================================
// 通用审计接口
// 设计文档要求核心表具备审计追踪能力，因此实体统一暴露 CreatedAt / UpdatedAt。
// DbContext.SaveChanges 会自动维护这两个字段。
// ============================================================
public interface IAuditableEntity
{
    /// <summary>
    /// 创建时间。使用 UTC 时间保存，展示时再按本地时区转换。
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最后更新时间。新增时等于 CreatedAt，修改时由 DbContext 自动刷新。
    /// </summary>
    DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 用户角色枚举。
/// 与数据库 Users.Role 字段对应：0=Admin, 1=Customer, 2=Merchant, 3=CustomerService。
/// EF Core 中统一按 NUMBER(10) / int 存储。
/// </summary>
public enum UserRole
{
    /// <summary>系统管理员。</summary>
    Admin = 0,

    /// <summary>普通顾客。</summary>
    Customer = 1,

    /// <summary>商家账号。注意：用户角色是 Merchant 不代表商家资料一定已审核通过。</summary>
    Merchant = 2,

    /// <summary>客服人员。</summary>
    CustomerService = 3
}

/// <summary>
/// 商家审核状态。
/// 与 Merchants.Status 字段对应：0=待审核, 1=已批准, 2=已拒绝。
/// </summary>
public enum MerchantStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

/// <summary>
/// 商品状态。
/// 与 Products.Status 字段对应：0=待审核, 1=已上架, 2=已下架, 3=已拒绝。
/// </summary>
public enum ProductStatus
{
    PendingReview = 0,
    Listed = 1,
    Delisted = 2,
    Rejected = 3
}

/// <summary>
/// 订单状态。
/// 与 Orders.Status 字段对应：0=待支付, 1=待发货, 2=已发货, 3=已完成, 4=已取消。
/// </summary>
public enum OrderStatus
{
    PendingPayment = 0,
    PendingShipment = 1,
    Shipped = 2,
    Completed = 3,
    Cancelled = 4
}

/// <summary>
/// 支付方式。
/// 与 Payments.PaymentMethod 字段对应：0=支付宝, 1=微信, 2=信用卡。
/// </summary>
public enum PaymentMethod
{
    Alipay = 0,
    WeChat = 1,
    CreditCard = 2
}

/// <summary>
/// 支付状态。
/// 与 Payments.Status 字段对应：0=待支付, 1=支付成功, 2=支付失败。
/// </summary>
public enum PaymentStatus
{
    Pending = 0,
    Success = 1,
    Failed = 2
}

/// <summary>
/// 客服工单状态。
/// 与 CustomerServiceTickets.Status 字段对应：0=待处理, 1=处理中, 2=已解决, 3=已关闭。
/// </summary>
public enum TicketStatus
{
    Pending = 0,
    Processing = 1,
    Resolved = 2,
    Closed = 3
}

// ============================================================
// 用户模型 (User)
// 系统中所有角色（管理员、顾客、商家、客服）均存储于此表
// 通过 Role 字段区分角色类型
// ============================================================

/// <summary>
/// 用户表实体，对应数据库表 Users。
///
/// 设计要点：
/// 1. 所有角色共用同一张用户表，使用 Role 区分身份。
/// 2. Username 唯一且必填，用于登录或展示。
/// 3. PasswordHash 存储 BCrypt 等加密后的密码摘要，不保存明文密码。
/// 4. Email 可空但唯一，Phone 可空。
/// 5. User 与 Merchant、ShoppingCart 都是一对一关系。
/// </summary>
[Table("Users")]
[Index(nameof(Username), IsUnique = true, Name = "UX_Users_Username")]
[Index(nameof(Email), IsUnique = true, Name = "UX_Users_Email")]
[Comment("用户表：存储管理员、顾客、商家、客服等所有账号信息。")]
public class User : IAuditableEntity
{
    /// <summary>
    /// 主键，自增。Oracle 侧建议映射为 NUMBER(19)。
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column(TypeName = "NUMBER(19)")]
    [Comment("主键，自增。")]
    public long Id { get; set; }

    /// <summary>
    /// 用户名，唯一且非空。
    /// </summary>
    [Required]
    [StringLength(50)]
    [Column(TypeName = "VARCHAR2(50)")]
    [Comment("用户名，唯一且不能为空。")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码哈希值。
    /// 这里不保存明文密码，业务层注册或修改密码时应写入 BCrypt / PBKDF2 / Argon2 等算法生成的哈希。
    /// </summary>
    [Required]
    [StringLength(255)]
    [Column(TypeName = "VARCHAR2(255)")]
    [Comment("加密后的密码哈希，不保存明文密码。")]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱。可空，但如果填写则需要唯一。
    /// </summary>
    [StringLength(100)]
    [EmailAddress]
    [Column(TypeName = "VARCHAR2(100)")]
    [Comment("邮箱，可空，填写时要求唯一。")]
    public string? Email { get; set; }

    /// <summary>
    /// 手机号。可空。
    /// </summary>
    [StringLength(20)]
    [Phone]
    [Column(TypeName = "VARCHAR2(20)")]
    [Comment("手机号，可空。")]
    public string? Phone { get; set; }

    /// <summary>
    /// 用户角色。
    /// 默认创建为普通顾客，管理员、商家、客服应由后台或授权流程设置。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(10)")]
    [Comment("角色：0=Admin, 1=Customer, 2=Merchant, 3=CustomerService。")]
    public UserRole Role { get; set; } = UserRole.Customer;

    /// <summary>
    /// 账号是否启用。
    /// true 表示可以登录和正常使用；false 表示停用。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(1)")]
    [Comment("是否启用：1=是，0=否。")]
    public bool IsActive { get; set; } = true;

    /// <inheritdoc />
    [Required]
    [Column(TypeName = "TIMESTAMP")]
    [Comment("注册时间。")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <inheritdoc />
    [Required]
    [Column(TypeName = "TIMESTAMP")]
    [Comment("最后更新时间。")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // -----------------------------
    // 导航属性
    // -----------------------------

    /// <summary>
    /// 该用户对应的商家资料。
    /// 只有申请成为商家的用户才会存在此记录。
    /// </summary>
    [InverseProperty(nameof(Merchant.User))]
    public Merchant? Merchant { get; set; }

    /// <summary>
    /// 用户购物车。
    /// 设计要求每个用户最多拥有一个购物车。
    /// </summary>
    [InverseProperty(nameof(ShoppingCart.User))]
    public ShoppingCart? ShoppingCart { get; set; }

    /// <summary>
    /// 用户提交的订单集合。
    /// </summary>
    [InverseProperty(nameof(Order.User))]
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    /// <summary>
    /// 用户发表的商品评价集合。
    /// </summary>
    [InverseProperty(nameof(ProductReview.User))]
    public ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();

    /// <summary>
    /// 用户提交的客服工单集合。
    /// </summary>
    [InverseProperty(nameof(CustomerServiceTicket.User))]
    public ICollection<CustomerServiceTicket> SubmittedTickets { get; set; } = new List<CustomerServiceTicket>();

    /// <summary>
    /// 分配给该客服人员处理的工单集合。
    /// 只有 Role=CustomerService 的用户才应被分配工单。
    /// </summary>
    [InverseProperty(nameof(CustomerServiceTicket.AssignedCustomerService))]
    public ICollection<CustomerServiceTicket> AssignedTickets { get; set; } = new List<CustomerServiceTicket>();

    /// <summary>
    /// 判断当前用户是否为可用商家账号。
    /// 注意这里仅判断用户角色和启用状态；商家审核状态需要检查 Merchant.Status。
    /// </summary>
    [NotMapped]
    public bool IsMerchantAccount => IsActive && Role == UserRole.Merchant;
}
