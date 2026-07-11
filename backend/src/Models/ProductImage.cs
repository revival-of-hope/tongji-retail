#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RetailSystem.Backend.Models;

// ============================================================
// 商品图片模型 (ProductImage)
// 将图片信息从商品表中拆分，满足第三范式
// 一个商品可以有多张图片，其中一张为主图
// ============================================================

/// <summary>
/// 商品图片实体，对应数据库表 ProductImages。
///
/// 设计要点：
/// 1. 一个商品可以有多张图片。
/// 2. IsMain 标记是否主图；如需严格限制“每个商品只有一张主图”，建议在业务层控制，
///    或在数据库侧根据 Oracle 版本设计函数索引/触发器实现。
/// 3. SortOrder 用于控制图片展示顺序。
/// </summary>
[Table("ProductImages")]
[Index(nameof(ProductId), Name = "IX_ProductImages_ProductId")]
[Comment("商品图片表：一个商品可对应多张图片，其中一张可作为主图。")]
public class ProductImage : IAuditableEntity
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
    /// 所属商品 ID。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(19)")]
    [Comment("所属商品 ID。")]
    public long ProductId { get; set; }

    /// <summary>
    /// 图片 URL。
    /// 可保存对象存储地址、相对路径或 CDN 地址。
    /// </summary>
    [Required]
    [StringLength(500)]
    [Column(TypeName = "VARCHAR2(500)")]
    [Comment("图片链接。")]
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// 是否为主图。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(1)")]
    [Comment("是否为主图：1=是，0=否。")]
    public bool IsMain { get; set; } = false;

    /// <summary>
    /// 图片排序号。
    /// </summary>
    [Required]
    [Column(TypeName = "NUMBER(10)")]
    [Comment("排序号。")]
    public int SortOrder { get; set; } = 0;

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
    /// 所属商品。
    /// </summary>
    [ForeignKey(nameof(ProductId))]
    [InverseProperty(nameof(Product.Images))]
    public Product Product { get; set; } = null!;
}
