#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RetailSystem.Backend.Models;

// ============================================================
// 商品评价模型 (ProductReview)
// 业务规则：只有购买过该商品且订单已完成的用户才能评价
//
// 客服工单模型 (CustomerServiceTicket)
// 顾客提交工单后由客服处理，支持状态流转
// ============================================================

/// <summary>
/// 商品评价实体，对应数据库表 ProductReviews。
///
/// 设计要点：
/// 1. ProductId 指向被评价商品。
/// 2. UserId 指向评价人。
/// 3. OrderId 指向关联订单，业务层应校验订单已完成且用户确实购买过该商品。
/// 4. Rating 为 1-5 星。
/// </summary>
[Table("ProductReviews")]
[Index(nameof(ProductId), Name = "IX_ProductReviews_ProductId")]
[Index(nameof(UserId), Name = "IX_ProductReviews_UserId")]
[Index(nameof(OrderId), Name = "IX_ProductReviews_OrderId")]
[Index(nameof(UserId), nameof(OrderId), nameof(ProductId), IsUnique = true, Name = "UX_ProductReviews_User_Order_Product")]
[Comment("商品评价表：记录用户对已购买商品的评分和文字评价。")]
public class ProductReview : IAuditableEntity
{
    /// <summary>
    /// 主键，自增。
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column(TypeName = "NUMBER(19)")]
    [Comment("主键，自增。")]
    public long Id { get; set; }

    /// <summary>
    /// 被评价的商品 ID。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(19)")]
    [Comment("评价的商品 ID。")]
    public long ProductId { get; set; }

    /// <summary>
    /// 评价人用户 ID。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(19)")]
    [Comment("评价人用户 ID。")]
    public long UserId { get; set; }

    /// <summary>
    /// 关联订单 ID。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(19)")]
    [Comment("关联订单 ID。")]
    public long OrderId { get; set; }

    /// <summary>
    /// 评分，1-5 星。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(10)")]
    [Range(1, 5)]
    [Comment("评分，1-5 星。")]
    public int Rating { get; set; }

    /// <summary>
    /// 评价内容，可空。
    /// </summary>
    [StringLength(1000)]
    [Column(TypeName = "VARCHAR2(1000)")]
    [Comment("评价内容。")]
    public string? Comment { get; set; }

    /// <inheritdoc />
    [Required]
    [Column(TypeName = "TIMESTAMP")]
    [Comment("评价时间。")]
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
    /// 被评价商品。
    /// </summary>
    [ForeignKey(nameof(ProductId))]
    [InverseProperty(nameof(Product.Reviews))]
    public Product Product { get; set; } = null!;

    /// <summary>
    /// 评价人。
    /// </summary>
    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(User.ProductReviews))]
    public User User { get; set; } = null!;

    /// <summary>
    /// 关联订单。
    /// </summary>
    [ForeignKey(nameof(OrderId))]
    [InverseProperty(nameof(Order.Reviews))]
    public Order Order { get; set; } = null!;
}

/// <summary>
/// 客服工单实体，对应数据库表 CustomerServiceTickets。
///
/// 设计要点：
/// 1. UserId 为提交工单的顾客。
/// 2. OrderId 可空，因为有些咨询可能不关联订单。
/// 3. AssignedTo 可空，未分配客服时为空。
/// 4. UserId 和 AssignedTo 都引用 Users.Id，因此需要在 DbContext 中显式配置两条关系。
/// </summary>
[Table("CustomerServiceTickets")]
[Index(nameof(UserId), Name = "IX_CustomerServiceTickets_UserId")]
[Index(nameof(OrderId), Name = "IX_CustomerServiceTickets_OrderId")]
[Index(nameof(AssignedTo), Name = "IX_CustomerServiceTickets_AssignedTo")]
[Index(nameof(Status), Name = "IX_CustomerServiceTickets_Status")]
[Comment("客服工单表：记录顾客提交的问题及客服处理结果。")]
public class CustomerServiceTicket : IAuditableEntity
{
    /// <summary>
    /// 主键，自增。
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column(TypeName = "NUMBER(19)")]
    [Comment("主键，自增。")]
    public long Id { get; set; }

    /// <summary>
    /// 提交人用户 ID。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(19)")]
    [Comment("提交人用户 ID。")]
    public long UserId { get; set; }

    /// <summary>
    /// 关联订单 ID，可空。
    /// </summary>
    [Column(TypeName = "NUMBER(19)")]
    [Comment("关联订单 ID，可空。")]
    public long? OrderId { get; set; }

    /// <summary>
    /// 分配的客服人员用户 ID，可空。
    /// </summary>
    [Column(TypeName = "NUMBER(19)")]
    [Comment("分配的客服人员 ID，可空。")]
    public long? AssignedTo { get; set; }

    /// <summary>
    /// 工单主题。
    /// </summary>
    [Required]
    [StringLength(200)]
    [Column(TypeName = "VARCHAR2(200)")]
    [Comment("工单主题。")]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// 问题描述。
    /// </summary>
    [Required]
    [StringLength(2000)]
    [Column(TypeName = "VARCHAR2(2000)")]
    [Comment("问题描述。")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 客服回复内容，可空。
    /// </summary>
    [StringLength(2000)]
    [Column(TypeName = "VARCHAR2(2000)")]
    [Comment("客服回复内容。")]
    public string? Reply { get; set; }

    /// <summary>
    /// 工单状态。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(10)")]
    [Comment("状态：0=待处理, 1=处理中, 2=已解决, 3=已关闭。")]
    public TicketStatus Status { get; set; } = TicketStatus.Pending;

    /// <inheritdoc />
    [Required]
    [Column(TypeName = "TIMESTAMP")]
    [Comment("提交时间。")]
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
    /// 提交工单的用户。
    /// </summary>
    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(User.SubmittedTickets))]
    public User User { get; set; } = null!;

    /// <summary>
    /// 关联订单，可空。
    /// </summary>
    [ForeignKey(nameof(OrderId))]
    [InverseProperty(nameof(Order.Tickets))]
    public Order? Order { get; set; }

    /// <summary>
    /// 分配的客服人员，可空。
    /// </summary>
    [ForeignKey(nameof(AssignedTo))]
    [InverseProperty(nameof(User.AssignedTickets))]
    public User? AssignedCustomerService { get; set; }

    /// <summary>
    /// 分配客服。
    /// 调用前应在业务层校验 customerServiceUser.Role 是否为 CustomerService。
    /// </summary>
    public void AssignTo(User customerServiceUser)
    {
        if (customerServiceUser is null)
        {
            throw new ArgumentNullException(nameof(customerServiceUser));
        }

        if (customerServiceUser.Role != UserRole.CustomerService)
        {
            throw new InvalidOperationException("只能将工单分配给客服角色用户。");
        }

        AssignedTo = customerServiceUser.Id;
        AssignedCustomerService = customerServiceUser;
        Status = TicketStatus.Processing;
    }

    /// <summary>
    /// 客服回复并解决工单。
    /// </summary>
    public void Resolve(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
        {
            throw new ArgumentException("客服回复不能为空。", nameof(reply));
        }

        Reply = reply;
        Status = TicketStatus.Resolved;
    }

    /// <summary>
    /// 关闭工单。
    /// </summary>
    public void Close()
    {
        Status = TicketStatus.Closed;
    }
}
