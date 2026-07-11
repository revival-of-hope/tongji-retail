#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RetailSystem.Backend.Models;

// ============================================================
// 购物车模型 (ShoppingCart + CartItem)
// 每个用户拥有唯一一个购物车（一对一）
// 购物车明细（CartItem）记录每个商品的数量
// 拆分为两张表满足第三范式
// ============================================================

/// <summary>
/// 购物车实体，对应数据库表 ShoppingCarts。
///
/// 设计要点：
/// 1. 一个用户最多拥有一个购物车，因此 UserId 设置唯一索引。
/// 2. 购物车本身只保存所属用户和更新时间。
/// 3. 购物车中的商品项拆分为 CartItems，满足第三范式。
/// </summary>
[Table("ShoppingCarts")]
[Index(nameof(UserId), IsUnique = true, Name = "UX_ShoppingCarts_UserId")]
[Comment("购物车表：每个用户最多拥有一个购物车。")]
public class ShoppingCart : IAuditableEntity
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
    /// 所属用户 ID。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(19)")]
    [Comment("所属用户 ID，唯一。")]
    public long UserId { get; set; }

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

    // -----------------------------
    // 导航属性
    // -----------------------------

    /// <summary>
    /// 购物车所属用户。
    /// </summary>
    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(User.ShoppingCart))]
    public User User { get; set; } = null!;

    /// <summary>
    /// 购物车商品明细集合。
    /// </summary>
    [InverseProperty(nameof(CartItem.Cart))]
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();

    /// <summary>
    /// 向购物车中添加商品。
    /// 如果购物车中已存在该商品，则累加数量。
    /// 注意：该方法只维护实体集合；实际项目中还应在业务层校验商品是否上架、库存是否足够。
    /// </summary>
    public void AddItem(Product product, int quantity)
    {
        if (product is null)
        {
            throw new ArgumentNullException(nameof(product));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "加购数量必须大于 0。");
        }

        foreach (var item in Items)
        {
            if (item.ProductId == product.Id)
            {
                item.Quantity += quantity;
                UpdatedAt = DateTime.UtcNow;
                return;
            }
        }

        Items.Add(new CartItem
        {
            Cart = this,
            CartId = Id,
            Product = product,
            ProductId = product.Id,
            Quantity = quantity
        });

        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// 购物车明细实体，对应数据库表 CartItems。
///
/// 设计要点：
/// 1. CartId 指向购物车。
/// 2. ProductId 指向商品。
/// 3. 建议对 CartId + ProductId 设置唯一索引，避免同一购物车重复出现同一商品。
/// </summary>
[Table("CartItems")]
[Index(nameof(CartId), nameof(ProductId), IsUnique = true, Name = "UX_CartItems_CartId_ProductId")]
[Index(nameof(ProductId), Name = "IX_CartItems_ProductId")]
[Comment("购物车明细表：记录购物车中的商品及数量。")]
public class CartItem : IAuditableEntity
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
    /// 所属购物车 ID。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(19)")]
    [Comment("所属购物车 ID。")]
    public long CartId { get; set; }

    /// <summary>
    /// 商品 ID。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(19)")]
    [Comment("商品 ID。")]
    public long ProductId { get; set; }

    /// <summary>
    /// 加购数量。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(10)")]
    [Range(1, int.MaxValue)]
    [Comment("加购数量。")]
    public int Quantity { get; set; }

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
    /// 所属购物车。
    /// </summary>
    [ForeignKey(nameof(CartId))]
    [InverseProperty(nameof(ShoppingCart.Items))]
    public ShoppingCart Cart { get; set; } = null!;

    /// <summary>
    /// 对应商品。
    /// </summary>
    [ForeignKey(nameof(ProductId))]
    [InverseProperty(nameof(Product.CartItems))]
    public Product Product { get; set; } = null!;
}
