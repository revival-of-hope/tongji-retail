#nullable enable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RetailSystem.Backend.Models;

// ============================================================
// 商品分类模型 (Category)
// 支持多级分类（自引用），ParentId 为空时表示顶级分类
// ============================================================

/// <summary>
/// 商品分类实体，对应数据库表 Categories。
///
/// 设计要点：
/// 1. ParentId 可空；ParentId 为空表示顶级分类。
/// 2. 通过自引用关系支持多级分类树。
/// 3. SortOrder 用于同级分类排序。
/// </summary>
[Table("Categories")]
[Index(nameof(ParentId), Name = "IX_Categories_ParentId")]
[Index(nameof(Name), Name = "IX_Categories_Name")]
[Comment("商品分类表：支持多级分类树结构。")]
public class Category : IAuditableEntity
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
    /// 父分类 ID。
    /// 为空表示当前分类是顶级分类。
    /// </summary>
    [Column(TypeName = "NUMBER(19)")]
    [Comment("父分类 ID，可空；为空表示顶级分类。")]
    public long? ParentId { get; set; }

    /// <summary>
    /// 分类名称。
    /// </summary>
    [Required]
    [StringLength(50)]
    [Column(TypeName = "VARCHAR2(50)")]
    [Comment("分类名称。")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 排序号。
    /// 数值越小越靠前，具体排序规则由查询时 OrderBy 决定。
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

    // -----------------------------
    // 导航属性
    // -----------------------------

    /// <summary>
    /// 父分类。
    /// </summary>
    [ForeignKey(nameof(ParentId))]
    [InverseProperty(nameof(Children))]
    public Category? Parent { get; set; }

    /// <summary>
    /// 子分类集合。
    /// </summary>
    [InverseProperty(nameof(Parent))]
    public ICollection<Category> Children { get; set; } = new List<Category>();

    /// <summary>
    /// 当前分类下的商品集合。
    /// </summary>
    [InverseProperty(nameof(Product.Category))]
    public ICollection<Product> Products { get; set; } = new List<Product>();

    /// <summary>
    /// 是否为顶级分类。
    /// </summary>
    [NotMapped]
    public bool IsRoot => ParentId is null;
}
