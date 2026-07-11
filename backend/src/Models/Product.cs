#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RetailSystem.Backend.Models;

// ============================================================
// 商品模型 (Product)
// 商家发布的商品，需经过管理员审核后才能上架
// 与 ProductImage 是一对多关系（拆分图片表满足第三范式）
// ============================================================

/// <summary>
/// 商品实体，对应数据库表 Products。
///
/// 设计要点：
/// 1. 商品属于一个商家和一个分类。
/// 2. 商品图片拆分到 ProductImages 表，避免图片字段在商品表中重复或非原子化。
/// 3. Price 使用 decimal 并映射为 DECIMAL(18,2)，避免金额精度问题。
/// 4. AvgRating 使用 DECIMAL(3,2)，用于保存 0.00-5.00 的平均评分。
/// 5. 商品需要审核，只有 Listed 状态才适合展示给前台顾客。
/// </summary>
[Table("Products")]
[Index(nameof(MerchantId), Name = "IX_Products_MerchantId")]
[Index(nameof(CategoryId), Name = "IX_Products_CategoryId")]
[Index(nameof(Status), Name = "IX_Products_Status")]
[Index(nameof(Name), Name = "IX_Products_Name")]
[Comment("商品表：存储商品核心信息，商品图片拆分至 ProductImages。")]
public class Product : IAuditableEntity
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
    /// 所属商家 ID。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(19)")]
    [Comment("所属商家 ID。")]
    public long MerchantId { get; set; }

    /// <summary>
    /// 所属分类 ID。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(19)")]
    [Comment("所属分类 ID。")]
    public long CategoryId { get; set; }

    /// <summary>
    /// 商品名称。
    /// </summary>
    [Required]
    [StringLength(200)]
    [Column(TypeName = "VARCHAR2(200)")]
    [Comment("商品名称。")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 商品详情描述。
    /// 映射为 CLOB，适合保存较长文本。
    /// </summary>
    [Column(TypeName = "CLOB")]
    [Comment("商品详情描述。")]
    public string? Description { get; set; }

    /// <summary>
    /// 当前售价。
    /// 金额字段必须使用 decimal，不能使用 float/double。
    /// </summary>
    [Required]
    [Precision(18, 2)]
    [Column(TypeName = "DECIMAL(18,2)")]
    [Range(typeof(decimal), "0.00", "9999999999999999.99")]
    [Comment("当前售价，DECIMAL(18,2)。")]
    public decimal Price { get; set; }

    /// <summary>
    /// 当前库存数量。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(10)")]
    [Range(0, int.MaxValue)]
    [Comment("当前库存量。")]
    public int StockQuantity { get; set; }

    /// <summary>
    /// 累计销量。
    /// 通常由订单完成或发货逻辑累计维护。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(10)")]
    [Range(0, int.MaxValue)]
    [Comment("累计销量。")]
    public int SoldCount { get; set; }

    /// <summary>
    /// 平均评分，取值范围 0.00-5.00。
    /// 通常由 ProductReview 聚合计算后回写，便于商品列表快速查询。
    /// </summary>
    [Required]
    [Precision(3, 2)]
    [Column(TypeName = "DECIMAL(3,2)")]
    [Range(typeof(decimal), "0.00", "5.00")]
    [Comment("平均评分，0.00-5.00。")]
    public decimal AvgRating { get; set; } = 0m;

    /// <summary>
    /// 评价总数。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(10)")]
    [Range(0, int.MaxValue)]
    [Comment("评价总数。")]
    public int ReviewCount { get; set; }

    /// <summary>
    /// 商品状态。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(10)")]
    [Comment("状态：0=待审核, 1=已上架, 2=已下架, 3=已拒绝。")]
    public ProductStatus Status { get; set; } = ProductStatus.PendingReview;

    /// <inheritdoc />
    [Required]
    [Column(TypeName = "TIMESTAMP")]
    [Comment("发布时间。")]
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
    /// 所属商家。
    /// </summary>
    [ForeignKey(nameof(MerchantId))]
    [InverseProperty(nameof(Merchant.Products))]
    public Merchant Merchant { get; set; } = null!;

    /// <summary>
    /// 所属分类。
    /// </summary>
    [ForeignKey(nameof(CategoryId))]
    [InverseProperty(nameof(Category.Products))]
    public Category Category { get; set; } = null!;

    /// <summary>
    /// 商品图片集合。
    /// </summary>
    [InverseProperty(nameof(ProductImage.Product))]
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    /// <summary>
    /// 购物车明细集合。
    /// </summary>
    [InverseProperty(nameof(CartItem.Product))]
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    /// <summary>
    /// 订单明细集合。
    /// </summary>
    [InverseProperty(nameof(OrderItem.Product))]
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    /// <summary>
    /// 商品评价集合。
    /// </summary>
    [InverseProperty(nameof(ProductReview.Product))]
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();

    /// <summary>
    /// 是否已上架。
    /// </summary>
    [NotMapped]
    public bool IsListed => Status == ProductStatus.Listed;

    /// <summary>
    /// 库存是否充足。
    /// </summary>
    public bool HasEnoughStock(int quantity)
    {
        return quantity > 0 && StockQuantity >= quantity;
    }

    /// <summary>
    /// 扣减库存。
    /// 该方法只修改内存中的实体状态；实际并发控制建议在业务层结合事务或乐观并发处理。
    /// </summary>
    public void DecreaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "扣减数量必须大于 0。");
        }

        if (StockQuantity < quantity)
        {
            throw new InvalidOperationException("库存不足，无法扣减。");
        }

        StockQuantity -= quantity;
        SoldCount += quantity;
    }

    /// <summary>
    /// 审核通过并上架商品。
    /// </summary>
    public void ApproveAndList()
    {
        Status = ProductStatus.Listed;
    }

    /// <summary>
    /// 下架商品。
    /// </summary>
    public void Delist()
    {
        Status = ProductStatus.Delisted;
    }

    /// <summary>
    /// 拒绝商品上架申请。
    /// </summary>
    public void Reject()
    {
        Status = ProductStatus.Rejected;
    }
}
