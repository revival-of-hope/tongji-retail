#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RetailSystem.Backend.Models;

// ============================================================
// 商家模型 (Merchant)
// 一个用户可以申请成为商家，审核通过后才能发布商品
// 与 User 是一对一关系
// ============================================================

/// <summary>
/// 商家表实体，对应数据库表 Merchants。
///
/// 设计要点：
/// 1. 一个 User 最多对应一个 Merchant，因此 UserId 设置唯一索引。
/// 2. 商家入驻后需要审核，只有 Status=Approved 的商家才能发布和上架商品。
/// 3. Merchant 与 Product 是一对多关系。
/// </summary>
[Table("Merchants")]
[Index(nameof(UserId), IsUnique = true, Name = "UX_Merchants_UserId")]
[Comment("商家表：存储店铺信息及入驻审核状态。")]
public class Merchant : IAuditableEntity
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
    /// 关联用户 ID。
    /// 该字段唯一，保证一个用户最多只有一个商家资料。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(19)")]
    [Comment("关联用户 ID，唯一。")]
    public long UserId { get; set; }

    /// <summary>
    /// 店铺名称，必填。
    /// </summary>
    [Required]
    [StringLength(100)]
    [Column(TypeName = "VARCHAR2(100)")]
    [Comment("店铺名称。")]
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// 店铺描述，可空。
    /// </summary>
    [StringLength(500)]
    [Column(TypeName = "VARCHAR2(500)")]
    [Comment("店铺描述，可空。")]
    public string? Description { get; set; }

    /// <summary>
    /// 商家审核状态。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(10)")]
    [Comment("状态：0=待审核, 1=已批准, 2=已拒绝。")]
    public MerchantStatus Status { get; set; } = MerchantStatus.Pending;

    /// <inheritdoc />
    [Required]
    [Column(TypeName = "TIMESTAMP")]
    [Comment("申请时间。")]
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
    /// 关联的用户账号。
    /// </summary>
    [ForeignKey(nameof(UserId))]
    [InverseProperty(nameof(User.Merchant))]
    public User User { get; set; } = null!;

    /// <summary>
    /// 当前商家发布的商品集合。
    /// </summary>
    [InverseProperty(nameof(Product.Merchant))]
    public ICollection<Product> Products { get; set; } = new List<Product>();

    /// <summary>
    /// 判断商家是否已通过审核。
    /// </summary>
    [NotMapped]
    public bool IsApproved => Status == MerchantStatus.Approved;

    /// <summary>
    /// 审核通过商家。
    /// 通常由管理员在应用服务层调用。
    /// </summary>
    public void Approve()
    {
        Status = MerchantStatus.Approved;
    }

    /// <summary>
    /// 拒绝商家入驻申请。
    /// </summary>
    public void Reject()
    {
        Status = MerchantStatus.Rejected;
    }
}
