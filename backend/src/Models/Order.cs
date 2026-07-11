#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RetailSystem.Backend.Models;

// ============================================================
// 订单模型 (Order + OrderItem + Payment)
// 订单是核心业务实体，包含状态机流转逻辑
// OrderItem 拆分订单明细满足第三范式
// Payment 独立记录支付流水
// ============================================================

/// <summary>
/// 订单实体，对应数据库表 Orders。
///
/// 设计要点：
/// 1. OrderNo 是业务流水号，唯一且不可为空。
/// 2. TotalAmount 使用 DECIMAL(18,2) 保存订单金额。
/// 3. OrderItem 保存下单时商品价格快照，避免商品后续改价影响历史订单。
/// 4. Payment 独立保存支付流水，便于后续扩展退款、对账等功能。
/// 5. 订单状态按 PendingPayment -> PendingShipment -> Shipped -> Completed 流转，也可取消。
/// </summary>
[Table("Orders")]
[Index(nameof(UserId), Name = "IX_Orders_UserId")]
[Index(nameof(OrderNo), IsUnique = true, Name = "UX_Orders_OrderNo")]
[Index(nameof(Status), Name = "IX_Orders_Status")]
[Comment("订单表：记录用户提交的购买订单。")]
public class Order : IAuditableEntity
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
    /// 下单用户 ID。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(19)")]
    [Comment("下单用户 ID。")]
    public long UserId { get; set; }

    /// <summary>
    /// 订单编号，业务流水号。
    /// 建议由业务层生成，例如：yyyyMMddHHmmss + 随机数/雪花 ID。
    /// </summary>
    [Required]
    [StringLength(50)]
    [Column(TypeName = "VARCHAR2(50)")]
    [Comment("订单编号，业务流水号，唯一。")]
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 订单总金额。
    /// </summary>
    [Required]
    [Precision(18, 2)]
    [Column(TypeName = "DECIMAL(18,2)")]
    [Range(typeof(decimal), "0.00", "9999999999999999.99")]
    [Comment("订单总金额，DECIMAL(18,2)。")]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// 订单状态。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(10)")]
    [Comment("状态：0=待支付, 1=待发货, 2=已发货, 3=已完成, 4=已取消。")]
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;

    /// <summary>
    /// 收货地址。
    /// </summary>
    [Required]
    [StringLength(500)]
    [Column(TypeName = "VARCHAR2(500)")]
    [Comment("收货地址。")]
    public string ShippingAddress { get; set; } = string.Empty;

    /// <summary>
    /// 订单备注，可空。
    /// </summary>
    [StringLength(500)]
    [Column(TypeName = "VARCHAR2(500)")]
    [Comment("订单备注，可空。")]
    public string? Remark { get; set; }

    /// <summary>
    /// 支付超时时间。
    /// 超时后可由定时任务取消订单并释放库存。
    /// </summary>
    [Required]
    [Column(TypeName = "TIMESTAMP")]
    [Comment("支付超时时间。")]
    public DateTime ExpireAt { get; set; }

    /// <inheritdoc />
    [Required]
    [Column(TypeName = "TIMESTAMP")]
    [Comment("下单时间。")]
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
    /// 下单用户。
    /// </summary>
    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(User.Orders))]
    public User User { get; set; } = null!;

    /// <summary>
    /// 订单明细集合。
    /// </summary>
    [InverseProperty(nameof(OrderItem.Order))]
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    /// <summary>
    /// 支付记录。
    /// 文档中 Payment.OrderId 为唯一约束，因此订单和支付记录是一对一关系。
    /// </summary>
    [InverseProperty(nameof(Payment.Order))]
    public Payment? Payment { get; set; }

    /// <summary>
    /// 与该订单关联的商品评价集合。
    /// </summary>
    [InverseProperty(nameof(ProductReview.Order))]
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();

    /// <summary>
    /// 与该订单关联的客服工单集合。
    /// </summary>
    [InverseProperty(nameof(CustomerServiceTicket.Order))]
    public ICollection<CustomerServiceTicket> Tickets { get; set; } = new List<CustomerServiceTicket>();

    /// <summary>
    /// 订单是否已过支付截止时间。
    /// </summary>
    public bool IsExpired(DateTime utcNow)
    {
        return Status == OrderStatus.PendingPayment && utcNow >= ExpireAt;
    }

    /// <summary>
    /// 支付成功后，订单进入待发货状态。
    /// </summary>
    public void MarkAsPaid()
    {
        if (Status != OrderStatus.PendingPayment)
        {
            throw new InvalidOperationException("只有待支付订单才能标记为已支付。");
        }

        Status = OrderStatus.PendingShipment;
    }

    /// <summary>
    /// 商家发货后，订单进入已发货状态。
    /// </summary>
    public void MarkAsShipped()
    {
        if (Status != OrderStatus.PendingShipment)
        {
            throw new InvalidOperationException("只有待发货订单才能标记为已发货。");
        }

        Status = OrderStatus.Shipped;
    }

    /// <summary>
    /// 用户确认收货或系统自动确认后，订单进入已完成状态。
    /// </summary>
    public void Complete()
    {
        if (Status != OrderStatus.Shipped)
        {
            throw new InvalidOperationException("只有已发货订单才能完成。 ");
        }

        Status = OrderStatus.Completed;
    }

    /// <summary>
    /// 取消订单。
    /// 已完成订单不允许直接取消，如需售后退款应扩展退款模型。
    /// </summary>
    public void Cancel()
    {
        if (Status == OrderStatus.Completed)
        {
            throw new InvalidOperationException("已完成订单不能直接取消。 ");
        }

        if (Status == OrderStatus.Cancelled)
        {
            return;
        }

        Status = OrderStatus.Cancelled;
    }
}

/// <summary>
/// 订单明细实体，对应数据库表 OrderItems。
///
/// 设计要点：
/// 1. 每条记录表示订单中的一个商品项。
/// 2. UnitPrice 保存下单时的商品单价快照。
/// 3. SubTotal 保存下单时的小计金额，通常等于 UnitPrice * Quantity。
/// </summary>
[Table("OrderItems")]
[Index(nameof(OrderId), Name = "IX_OrderItems_OrderId")]
[Index(nameof(ProductId), Name = "IX_OrderItems_ProductId")]
[Comment("订单明细表：记录订单中的商品、数量和下单时价格快照。")]
public class OrderItem : IAuditableEntity
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
    /// 所属订单 ID。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(19)")]
    [Comment("所属订单 ID。")]
    public long OrderId { get; set; }

    /// <summary>
    /// 商品 ID。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(19)")]
    [Comment("商品 ID。")]
    public long ProductId { get; set; }

    /// <summary>
    /// 购买数量。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(10)")]
    [Range(1, int.MaxValue)]
    [Comment("购买数量。")]
    public int Quantity { get; set; }

    /// <summary>
    /// 下单时的单价，价格快照。
    /// </summary>
    [Required]
    [Precision(18, 2)]
    [Column(TypeName = "DECIMAL(18,2)")]
    [Range(typeof(decimal), "0.00", "9999999999999999.99")]
    [Comment("下单时的单价，价格快照。")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// 小计金额。
    /// </summary>
    [Required]
    [Precision(18, 2)]
    [Column(TypeName = "DECIMAL(18,2)")]
    [Range(typeof(decimal), "0.00", "9999999999999999.99")]
    [Comment("小计金额，通常等于 UnitPrice * Quantity。")]
    public decimal SubTotal { get; set; }

    /// <inheritdoc />
    [Required]
    [Column(TypeName = "TIMESTAMP")]
    [Comment("创建时间。")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <inheritdoc />
    [Required]
    [Column(TypeName = "TIMESTAMP")]
    [Comment("最后更新时间。")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 所属订单。
    /// </summary>
    [ForeignKey(nameof(OrderId))]
    [InverseProperty(nameof(Order.Items))]
    public Order Order { get; set; } = null!;

    /// <summary>
    /// 对应商品。
    /// </summary>
    [ForeignKey(nameof(ProductId))]
    [InverseProperty(nameof(Product.OrderItems))]
    public Product Product { get; set; } = null!;

    /// <summary>
    /// 按单价和数量重新计算小计。
    /// </summary>
    public void RecalculateSubTotal()
    {
        if (Quantity <= 0)
        {
            throw new InvalidOperationException("购买数量必须大于 0。 ");
        }

        if (UnitPrice < 0)
        {
            throw new InvalidOperationException("商品单价不能小于 0。 ");
        }

        SubTotal = UnitPrice * Quantity;
    }
}

/// <summary>
/// 支付记录实体，对应数据库表 Payments。
///
/// 设计要点：
/// 1. Payment.OrderId 唯一，因此一个订单最多对应一条支付记录。
/// 2. Amount 保存实际支付金额。
/// 3. TransactionId 保存第三方支付平台返回的交易流水号，可空。
/// </summary>
[Table("Payments")]
[Index(nameof(OrderId), IsUnique = true, Name = "UX_Payments_OrderId")]
[Index(nameof(TransactionId), Name = "IX_Payments_TransactionId")]
[Comment("支付记录表：记录订单支付流水。")]
public class Payment : IAuditableEntity
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
    /// 关联订单 ID。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(19)")]
    [Comment("关联订单 ID，唯一。")]
    public long OrderId { get; set; }

    /// <summary>
    /// 实际支付金额。
    /// </summary>
    [Required]
    [Precision(18, 2)]
    [Column(TypeName = "DECIMAL(18,2)")]
    [Range(typeof(decimal), "0.00", "9999999999999999.99")]
    [Comment("实际支付金额。")]
    public decimal Amount { get; set; }

    /// <summary>
    /// 支付方式。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(10)")]
    [Comment("支付方式：0=支付宝, 1=微信, 2=信用卡。")]
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// 支付状态。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(10)")]
    [Comment("状态：0=待支付, 1=支付成功, 2=支付失败。")]
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>
    /// 第三方交易流水号。
    /// </summary>
    [StringLength(100)]
    [Column(TypeName = "VARCHAR2(100)")]
    [Comment("第三方交易流水号。")]
    public string? TransactionId { get; set; }

    /// <summary>
    /// 支付完成时间。
    /// 支付未完成时为空。
    /// </summary>
    [Column(TypeName = "TIMESTAMP")]
    [Comment("支付完成时间。")]
    public DateTime? PaidAt { get; set; }

    /// <inheritdoc />
    [Required]
    [Column(TypeName = "TIMESTAMP")]
    [Comment("创建时间。")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <inheritdoc />
    [Required]
    [Column(TypeName = "TIMESTAMP")]
    [Comment("最后更新时间。")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 关联订单。
    /// </summary>
    [ForeignKey(nameof(OrderId))]
    [InverseProperty(nameof(Order.Payment))]
    public Order Order { get; set; } = null!;

    /// <summary>
    /// 标记支付成功，并记录第三方流水号和支付完成时间。
    /// </summary>
    public void MarkAsSucceeded(string? transactionId = null, DateTime? paidAt = null)
    {
        Status = PaymentStatus.Success;
        TransactionId = transactionId;
        PaidAt = paidAt ?? DateTime.UtcNow;
    }

    /// <summary>
    /// 标记支付失败。
    /// </summary>
    public void MarkAsFailed()
    {
        Status = PaymentStatus.Failed;
        PaidAt = null;
    }
}
