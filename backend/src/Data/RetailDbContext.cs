// 本文件定义商品零售管理系统的 EF Core 数据库上下文 RetailDbContext。
#nullable enable

// 引入 EF Core 核心命名空间，用于 DbContext、DbSet、ModelBuilder、EntityState 等 ORM 基础类型。
using Microsoft.EntityFrameworkCore;
// 引入实体类型配置构建器 EntityTypeBuilder<T>，用于通过 Fluent API 配置表、字段、索引和关系。
using Microsoft.EntityFrameworkCore.Metadata.Builders;
// 引入业务实体模型命名空间，下面的 User、Product、Order 等实体类均来自该命名空间。
using RetailSystem.Backend.Models;

// 当前类位于 Data 命名空间中，用于存放数据库访问层和上下文配置代码。
namespace RetailSystem.Backend.Data;

/// <summary>
/// 商品零售管理系统数据库上下文。
/// RetailDbContext 继承自 EF Core 的 DbContext，是应用程序访问数据库的统一入口。
/// 它负责暴露 DbSet、维护实体跟踪状态，并在 OnModelCreating 中完成数据库映射配置。
///
/// 适用环境：
/// 1. .NET 8 / .NET 9 项目均可使用本写法。
/// 2. EF Core 9。
/// 3. Oracle 21c 时建议安装 Oracle.EntityFrameworkCore，并在 Program.cs 中调用 UseOracle。
///
/// 使用示例：
/// services.AddDbContext&lt;RetailDbContext&gt;(options =&gt;
///     options.UseOracle(configuration.GetConnectionString("Oracle")));
/// </summary>
public class RetailDbContext : DbContext
{
    /// <summary>
    /// 构造函数。
    /// DbContextOptions 由依赖注入容器注入。
    /// </summary>
    public RetailDbContext(DbContextOptions<RetailDbContext> options)
        // 将 options 传递给 DbContext 基类，使 EF Core 能够按 Program.cs 中配置的数据库提供程序工作。
        : base(options)
    {
    }

    // ============================================================
    // DbSet：对应数据库中的 12 张核心业务表
    // ============================================================

    /// <summary>Users 表访问入口：存储系统所有角色的账号信息，包括管理员、顾客、商家和客服。</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Merchants 表访问入口：存储商家店铺资料和入驻审核状态。</summary>
    public DbSet<Merchant> Merchants => Set<Merchant>();

    /// <summary>Categories 表访问入口：存储商品分类，并支持父子分类形成多级树结构。</summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>Products 表访问入口：存储商品名称、价格、库存、销量、评分和状态等核心信息。</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>ProductImages 表访问入口：存储商品图片链接、主图标记和图片排序信息。</summary>
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    /// <summary>ShoppingCarts 表访问入口：存储用户购物车主记录，通常一个用户对应一个购物车。</summary>
    public DbSet<ShoppingCart> ShoppingCarts => Set<ShoppingCart>();

    /// <summary>CartItems 表访问入口：存储购物车中每个商品项及其加购数量。</summary>
    public DbSet<CartItem> CartItems => Set<CartItem>();

    /// <summary>Orders 表访问入口：存储用户订单主信息，包括订单号、金额、状态和收货信息。</summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>OrderItems 表访问入口：存储订单明细及下单时价格快照。</summary>
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    /// <summary>Payments 表访问入口：存储订单支付记录、支付方式、支付状态和交易流水号。</summary>
    public DbSet<Payment> Payments => Set<Payment>();

    /// <summary>ProductReviews 表访问入口：存储用户对已购商品的评分和文字评价。</summary>
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();

    /// <summary>CustomerServiceTickets 表访问入口：存储用户售后问题、客服分配和处理回复。</summary>
    public DbSet<CustomerServiceTicket> CustomerServiceTickets => Set<CustomerServiceTicket>();

    /// <summary>
    /// 模型创建入口。
    /// 虽然实体类中已经使用 Data Annotation 标明了大量映射信息，
    /// 但主外键关系、删除行为、唯一索引、枚举转换等更适合在 Fluent API 中集中维护。
    /// OnModelCreating 会在 EF Core 构建模型时执行，用于补充或覆盖实体类上的 Data Annotation 配置。
    /// </summary> 
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 先调用基类实现，保留 EF Core 默认模型构建行为，再追加本项目的业务映射规则。
        base.OnModelCreating(modelBuilder);

        // 配置 Users 表：登录账号、角色、启用状态、唯一用户名/邮箱等。
        ConfigureUser(modelBuilder.Entity<User>());
        // 配置 Merchants 表：商家店铺信息、入驻审核状态以及与用户的一对一关系。
        ConfigureMerchant(modelBuilder.Entity<Merchant>());
        // 配置 Categories 表：商品分类及父子分类树形结构。
        ConfigureCategory(modelBuilder.Entity<Category>());
        // 配置 Products 表：商品基础信息、价格库存、评分销量以及上下架审核状态。
        ConfigureProduct(modelBuilder.Entity<Product>());
        // 配置 ProductImages 表：商品图片地址、主图标记及图片排序。
        ConfigureProductImage(modelBuilder.Entity<ProductImage>());
        // 配置 ShoppingCarts 表：用户与购物车的一对一关系。
        ConfigureShoppingCart(modelBuilder.Entity<ShoppingCart>());
        // 配置 CartItems 表：购物车中的商品项、数量以及购物车与商品的关联。
        ConfigureCartItem(modelBuilder.Entity<CartItem>());
        // 配置 Orders 表：订单主表，包括订单号、总金额、状态、收货地址和支付超时时间。
        ConfigureOrder(modelBuilder.Entity<Order>());
        // 配置 OrderItems 表：订单明细，保存商品数量、下单单价和小计金额等价格快照信息。
        ConfigureOrderItem(modelBuilder.Entity<OrderItem>());
        // 配置 Payments 表：订单支付流水、支付方式、支付状态、第三方交易号和支付时间。
        ConfigurePayment(modelBuilder.Entity<Payment>());
        // 配置 ProductReviews 表：用户对订单商品的评分和评价内容。
        ConfigureProductReview(modelBuilder.Entity<ProductReview>());
        // 配置 CustomerServiceTickets 表：售后/客服工单、处理状态和分配客服。
        ConfigureCustomerServiceTicket(modelBuilder.Entity<CustomerServiceTicket>());
    }

    /// <summary>
    /// 保存前自动写入审计字段。
    /// 新增实体：CreatedAt / UpdatedAt 均写入当前 UTC 时间。
    /// 修改实体：保留 CreatedAt，只刷新 UpdatedAt。
    /// 重写同步保存方法，在真正写入数据库前统一处理审计字段。
    /// </summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        // 保存前扫描新增和修改的实体，自动设置 CreatedAt、UpdatedAt。
        ApplyAuditInfo();
        // 调用 EF Core 原始保存逻辑，将变更提交到数据库。
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>
    /// 保存前自动写入审计字段。
    /// 重写异步保存方法，确保异步写库时也能执行同样的审计字段维护逻辑。
    /// </summary>
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        // 保存前扫描新增和修改的实体，自动设置 CreatedAt、UpdatedAt。
        ApplyAuditInfo();
        // 调用 EF Core 异步保存逻辑，并传入取消令牌以支持请求取消或超时控制。
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// 审计字段维护逻辑。
    /// 该私有方法只负责审计字段，不直接处理任何业务字段，便于所有实体复用。
    /// </summary>
    private void ApplyAuditInfo()
    {
        // 使用 UTC 时间作为统一时间基准，避免服务器部署在不同时区时产生时间混乱。
        var now = DateTime.UtcNow;

        // 仅遍历实现 IAuditableEntity 接口的实体，说明只有核心业务表会自动维护审计字段。
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            // Added 表示实体是本次新增，需要同时初始化创建时间和更新时间。
            if (entry.State == EntityState.Added)
            {
                // 如果业务层没有主动指定创建时间，则由上下文统一写入当前时间。
                if (entry.Entity.CreatedAt == default)
                {
                    // 写入创建时间，表示该记录首次进入系统的时间点。
                    entry.Entity.CreatedAt = now;
                }

                // 每次修改都更新 UpdatedAt，便于后续追踪数据最后变更时间。
                entry.Entity.UpdatedAt = now;
            }
            // Modified 表示实体是本次修改，只刷新更新时间，不改变原创建时间。
            else if (entry.State == EntityState.Modified)
            {
                // 创建时间一旦写入，不允许普通修改操作覆盖。
                entry.Property(entity => entity.CreatedAt).IsModified = false;
                // 每次修改都更新 UpdatedAt，便于后续追踪数据最后变更时间。
                entry.Entity.UpdatedAt = now;
            }
        }
    }

    /// <summary>
    /// 配置所有可审计实体共有的 CreatedAt 与 UpdatedAt 字段。
    /// 该方法通过泛型约束限定只作用于实现 IAuditableEntity 的实体。
    /// </summary>
    private static void ConfigureAudit<TEntity>(EntityTypeBuilder<TEntity> entity)
        // TEntity 必须是引用类型并实现 IAuditableEntity，保证实体一定包含 CreatedAt / UpdatedAt 属性。
        where TEntity : class, IAuditableEntity
    {
        entity.Property(e => e.CreatedAt)
            .HasColumnType("TIMESTAMP")
            .HasDefaultValueSql("SYSTIMESTAMP")
            .IsRequired();

        entity.Property(e => e.UpdatedAt)
            .HasColumnType("TIMESTAMP")
            .HasDefaultValueSql("SYSTIMESTAMP")
            .IsRequired();
    }

    /// <summary>
    /// 配置用户表 Users 的字段类型、主键、唯一约束、角色/启用状态检查约束以及审计字段。
    /// </summary>
    private static void ConfigureUser(EntityTypeBuilder<User> entity)
    {
        // 将 User 实体映射到 Users 表，并在表级别配置角色和启用状态的合法取值范围。
        entity.ToTable("Users", table =>
        {
            // 角色检查约束：仅允许 0=Admin、1=Customer、2=Merchant、3=CustomerService，防止写入非法角色值。
            table.HasCheckConstraint("CK_Users_Role", "Role IN (0, 1, 2, 3)");
            // 启用状态检查约束：只允许 0 或 1，配合 bool 到 NUMBER(1) 的转换。
            table.HasCheckConstraint("CK_Users_IsActive", "IsActive IN (0, 1)");
        });

        // 指定 Id 为用户表主键，用于唯一标识每个系统账号。
        entity.HasKey(e => e.Id);

        // 配置用户主键字段：Oracle NUMBER(19) 对应 C# long，适合承载较大数据量。
        entity.Property(e => e.Id)
            .HasColumnType("NUMBER(19)")
            .ValueGeneratedOnAdd();

        // 配置用户名字段：作为登录名使用，要求非空并限制长度，后续配置唯一索引。
        entity.Property(e => e.Username)
            .HasColumnType("VARCHAR2(50)")
            .HasMaxLength(50)
            .IsRequired();

        // 配置密码哈希字段：保存 BCrypt 等算法生成的密文，不保存明文密码。
        entity.Property(e => e.PasswordHash)
            .HasColumnType("VARCHAR2(255)")
            .HasMaxLength(255)
            .IsRequired();

        // 配置邮箱字段：可用于登录、通知或找回密码；字段可空，但如果填写则需要唯一。
        entity.Property(e => e.Email)
            .HasColumnType("VARCHAR2(100)")
            .HasMaxLength(100);

        // 配置手机号字段：用于联系方式或短信验证，最大长度预留国家区号等场景。
        entity.Property(e => e.Phone)
            .HasColumnType("VARCHAR2(20)")
            .HasMaxLength(20);

        // 配置用户角色枚举：通过 HasConversion<int>() 将枚举值保存为数字，便于数据库约束和查询。
        entity.Property(e => e.Role)
            .HasConversion<int>()
            .HasColumnType("NUMBER(10)")
            .IsRequired();

        // 配置账号启用状态：将布尔值转换为 0/1 存储，便于 Oracle NUMBER(1) 表达。
        entity.Property(e => e.IsActive)
            .HasConversion<int>()
            .HasColumnType("NUMBER(1)")
            .IsRequired();

        // 用户名唯一索引：避免出现重复登录账号，并提升按用户名查询的效率。
        entity.HasIndex(e => e.Username)
            .IsUnique()
            .HasDatabaseName("UX_Users_Username");

        // 邮箱唯一索引：避免多个账号绑定同一邮箱，同时提升按邮箱检索用户的效率。
        entity.HasIndex(e => e.Email)
            .IsUnique()
            .HasDatabaseName("UX_Users_Email");

        // 为用户表统一配置 CreatedAt、UpdatedAt 审计字段。
        ConfigureAudit(entity);
    }

    /// <summary>
    /// 配置商家表 Merchants，重点描述商家与用户账号的一对一关系以及商家审核状态。
    /// </summary>
    private static void ConfigureMerchant(EntityTypeBuilder<Merchant> entity)
    {
        // 将 Merchant 实体映射到 Merchants 表，并配置商家审核状态的合法范围。
        entity.ToTable("Merchants", table =>
        {
            // 商家状态检查约束：0=待审核、1=已批准、2=已拒绝。
            table.HasCheckConstraint("CK_Merchants_Status", "Status IN (0, 1, 2)");
        });

        // 指定 Id 为商家表主键，用于唯一标识每个店铺入驻记录。
        entity.HasKey(e => e.Id);

        // 配置商家主键字段，使用 Oracle NUMBER(19) 并由数据库/EF 生成新值。
        entity.Property(e => e.Id)
            .HasColumnType("NUMBER(19)")
            .ValueGeneratedOnAdd();

        // 配置关联用户 ID：每个商家必须绑定一个系统用户账号。
        entity.Property(e => e.UserId)
            .HasColumnType("NUMBER(19)")
            .IsRequired();

        // 配置店铺名称：作为前台展示和后台审核的重要字段，不能为空。
        entity.Property(e => e.StoreName)
            .HasColumnType("VARCHAR2(100)")
            .HasMaxLength(100)
            .IsRequired();

        // 配置店铺描述：用于展示商家介绍，允许为空并限制最大长度。
        entity.Property(e => e.Description)
            .HasColumnType("VARCHAR2(500)")
            .HasMaxLength(500);

        // 配置商家审核状态枚举：以整数形式保存，便于数据库检查约束生效。
        entity.Property(e => e.Status)
            .HasConversion<int>()
            .HasColumnType("NUMBER(10)")
            .IsRequired();

        // UserId 唯一索引：保证一个用户最多只能对应一个商家店铺记录。
        entity.HasIndex(e => e.UserId)
            .IsUnique()
            .HasDatabaseName("UX_Merchants_UserId");

        // 配置商家到用户的一对一关系：Merchant.User 指向其绑定的账号。
        entity.HasOne(e => e.User)
            // 配置用户侧导航属性 User.Merchant，表达一个用户最多拥有一个商家身份。
            .WithOne(e => e.Merchant)
            // Merchant.UserId 是外键，引用 Users.Id。
            .HasForeignKey<Merchant>(e => e.UserId)
            // Restrict 防止删除用户时自动删除商家记录，避免误删店铺资料。
            .OnDelete(DeleteBehavior.Restrict);

        // 为商家表统一配置 CreatedAt、UpdatedAt 审计字段。
        ConfigureAudit(entity);
    }

    /// <summary>
    /// 配置商品分类表 Categories，支持 ParentId 自关联以表达多级分类层级。
    /// </summary>
    private static void ConfigureCategory(EntityTypeBuilder<Category> entity)
    {
        // 将 Category 实体映射到 Categories 表，用于维护商品分类信息。
        entity.ToTable("Categories");

        // 指定 Id 为分类表主键。
        entity.HasKey(e => e.Id);

        // 配置分类主键字段，支持数据库生成自增标识。
        entity.Property(e => e.Id)
            .HasColumnType("NUMBER(19)")
            .ValueGeneratedOnAdd();

        // 配置父分类 ID：允许为空，空值表示顶级分类。
        entity.Property(e => e.ParentId)
            .HasColumnType("NUMBER(19)");

        // 配置分类名称：分类展示和检索使用，不能为空。
        entity.Property(e => e.Name)
            .HasColumnType("VARCHAR2(50)")
            .HasMaxLength(50)
            .IsRequired();

        // 配置排序号：用于同级分类在页面或后台管理中的显示顺序。
        entity.Property(e => e.SortOrder)
            .HasColumnType("NUMBER(10)")
            .IsRequired();

        // ParentId 索引用于快速查询某个分类下的子分类。
        entity.HasIndex(e => e.ParentId)
            .HasDatabaseName("IX_Categories_ParentId");

        // Name 索引用于提升按分类名称查询或模糊检索时的性能。
        entity.HasIndex(e => e.Name)
            .HasDatabaseName("IX_Categories_Name");

        // 配置分类自关联：当前分类可以拥有一个父分类。
        entity.HasOne(e => e.Parent)
            // 父分类可以拥有多个子分类，形成树形层级结构。
            .WithMany(e => e.Children)
            // ParentId 外键引用同一张 Categories 表的 Id。
            .HasForeignKey(e => e.ParentId)
            // Restrict 避免删除父分类时级联删除所有子分类，防止分类树被误删。
            .OnDelete(DeleteBehavior.Restrict);

        // 为分类表统一配置 CreatedAt、UpdatedAt 审计字段。
        ConfigureAudit(entity);
    }

    /// <summary>
    /// 配置商品表 Products，包括商家、分类外键，价格库存约束，评分销量默认值以及商品状态。
    /// </summary>
    private static void ConfigureProduct(EntityTypeBuilder<Product> entity)
    {
        // 将 Product 实体映射到 Products 表，并集中配置商品数值字段和状态字段的检查约束。
        entity.ToTable("Products", table =>
        {
            // 价格不能为负，保证商品售价的业务合理性。
            table.HasCheckConstraint("CK_Products_Price", "Price >= 0");
            // 库存数量不能为负，避免出现不合法库存。
            table.HasCheckConstraint("CK_Products_StockQuantity", "StockQuantity >= 0");
            // 累计销量不能为负。
            table.HasCheckConstraint("CK_Products_SoldCount", "SoldCount >= 0");
            // 平均评分限制在 0 到 5 之间，符合商品评分业务规则。
            table.HasCheckConstraint("CK_Products_AvgRating", "AvgRating >= 0 AND AvgRating <= 5");
            // 评价数量不能为负。
            table.HasCheckConstraint("CK_Products_ReviewCount", "ReviewCount >= 0");
            // 商品状态限制为 0=待审核、1=已上架、2=已下架、3=已拒绝。
            table.HasCheckConstraint("CK_Products_Status", "Status IN (0, 1, 2, 3)");
        });

        // 指定 Id 为商品表主键。
        entity.HasKey(e => e.Id);

        // 配置商品主键字段。
        entity.Property(e => e.Id)
            .HasColumnType("NUMBER(19)")
            .ValueGeneratedOnAdd();

        // 配置所属商家 ID：每个商品必须归属于一个商家。
        entity.Property(e => e.MerchantId)
            .HasColumnType("NUMBER(19)")
            .IsRequired();

        // 配置所属分类 ID：每个商品必须归属于一个商品分类。
        entity.Property(e => e.CategoryId)
            .HasColumnType("NUMBER(19)")
            .IsRequired();

        // 配置商品名称：用于展示、搜索和后台管理，不能为空。
        entity.Property(e => e.Name)
            .HasColumnType("VARCHAR2(200)")
            .HasMaxLength(200)
            .IsRequired();

        // 配置商品详情描述：使用 CLOB 以支持较长的富文本或详细介绍内容。
        entity.Property(e => e.Description)
            .HasColumnType("CLOB");

        // 配置商品当前售价：使用 DECIMAL(18,2) 保证金额精度。
        entity.Property(e => e.Price)
            .HasColumnType("DECIMAL(18,2)")
            .HasPrecision(18, 2)
            .IsRequired();

        // 配置当前库存数量。
        entity.Property(e => e.StockQuantity)
            .HasColumnType("NUMBER(10)")
            .IsRequired();

        // 配置累计销量，默认从 0 开始。
        entity.Property(e => e.SoldCount)
            .HasColumnType("NUMBER(10)")
            .HasDefaultValue(0)
            .IsRequired();

        // 配置平均评分，默认 0.00，精度为 3 位总长度、2 位小数。
        entity.Property(e => e.AvgRating)
            .HasColumnType("DECIMAL(3,2)")
            .HasPrecision(3, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        // 配置评价总数，默认从 0 开始。
        entity.Property(e => e.ReviewCount)
            .HasColumnType("NUMBER(10)")
            .HasDefaultValue(0)
            .IsRequired();

        // 配置商品状态枚举，默认值为待审核，符合商家发布后需平台审核的流程。
        entity.Property(e => e.Status)
            .HasConversion<int>()
            .HasColumnType("NUMBER(10)")
            .HasDefaultValue(ProductStatus.PendingReview)
            .IsRequired();

        // MerchantId 索引用于快速查询某个商家发布的所有商品。
        entity.HasIndex(e => e.MerchantId)
            .HasDatabaseName("IX_Products_MerchantId");

        // CategoryId 索引用于按分类筛选商品。
        entity.HasIndex(e => e.CategoryId)
            .HasDatabaseName("IX_Products_CategoryId");

        // Status 索引用于按商品审核/上下架状态筛选。
        entity.HasIndex(e => e.Status)
            .HasDatabaseName("IX_Products_Status");

        // Name 索引用于提升按商品名称搜索的性能。
        entity.HasIndex(e => e.Name)
            .HasDatabaseName("IX_Products_Name");

        // 配置商品到商家的一对多关系：一个商品属于一个商家。
        entity.HasOne(e => e.Merchant)
            // 商家侧可以通过 Products 导航属性访问其发布的多个商品。
            // 分类侧可以通过 Products 导航属性访问该分类下的多个商品。
            .WithMany(e => e.Products)
            // MerchantId 外键引用 Merchants.Id。
            .HasForeignKey(e => e.MerchantId)
            // Restrict 防止删除商家或分类时级联删除商品，保护交易相关数据。
            .OnDelete(DeleteBehavior.Restrict);

        // 配置商品到分类的一对多关系：一个商品属于一个分类。
        entity.HasOne(e => e.Category)
            // 商家侧可以通过 Products 导航属性访问其发布的多个商品。
            // 分类侧可以通过 Products 导航属性访问该分类下的多个商品。
            .WithMany(e => e.Products)
            // CategoryId 外键引用 Categories.Id。
            .HasForeignKey(e => e.CategoryId)
            // Restrict 防止删除商家或分类时级联删除商品，保护交易相关数据。
            .OnDelete(DeleteBehavior.Restrict);

        // 为商品表统一配置 CreatedAt、UpdatedAt 审计字段。
        ConfigureAudit(entity);
    }

    /// <summary>
    /// 配置商品图片表 ProductImages，包括图片地址、主图标记、排序字段以及与商品的一对多关系。
    /// </summary>
    private static void ConfigureProductImage(EntityTypeBuilder<ProductImage> entity)
    {
        // 将 ProductImage 实体映射到 ProductImages 表，并限制主图标记只能为 0/1。
        entity.ToTable("ProductImages", table =>
        {
            // IsMain 检查约束：1 表示主图，0 表示普通图片。
            table.HasCheckConstraint("CK_ProductImages_IsMain", "IsMain IN (0, 1)");
        });

        // 指定 Id 为商品图片表主键。
        entity.HasKey(e => e.Id);

        // 配置图片记录主键字段。
        entity.Property(e => e.Id)
            .HasColumnType("NUMBER(19)")
            .ValueGeneratedOnAdd();

        // 配置所属商品 ID：每张图片必须绑定一个商品。
        entity.Property(e => e.ProductId)
            .HasColumnType("NUMBER(19)")
            .IsRequired();

        // 配置图片链接地址：保存图片访问 URL 或对象存储路径。
        entity.Property(e => e.ImageUrl)
            .HasColumnType("VARCHAR2(500)")
            .HasMaxLength(500)
            .IsRequired();

        // 配置是否主图：将布尔值转换为 0/1 存储。
        entity.Property(e => e.IsMain)
            .HasConversion<int>()
            .HasColumnType("NUMBER(1)")
            .IsRequired();

        // 配置图片排序号：用于控制商品详情页图片展示顺序。
        entity.Property(e => e.SortOrder)
            .HasColumnType("NUMBER(10)")
            .IsRequired();

        // ProductId 索引用于快速加载某个商品的全部图片。
        entity.HasIndex(e => e.ProductId)
            .HasDatabaseName("IX_ProductImages_ProductId");

        // 配置图片到商品的一对多关系：一张图片属于一个商品。
        entity.HasOne(e => e.Product)
            // 商品侧通过 Images 导航属性访问多张图片。
            .WithMany(e => e.Images)
            // ProductId 外键引用 Products.Id。
            .HasForeignKey(e => e.ProductId)
            // Cascade 表示删除商品时自动删除其图片记录，避免残留无主图片数据。
            .OnDelete(DeleteBehavior.Cascade);

        // 为商品图片表统一配置 CreatedAt、UpdatedAt 审计字段。
        ConfigureAudit(entity);
    }

    /// <summary>
    /// 配置购物车表 ShoppingCarts，保证每个用户最多拥有一个购物车。
    /// </summary>
    private static void ConfigureShoppingCart(EntityTypeBuilder<ShoppingCart> entity)
    {
        // 将 ShoppingCart 实体映射到 ShoppingCarts 表，用于保存用户购物车主记录。
        entity.ToTable("ShoppingCarts");

        // 指定 Id 为购物车表主键。
        entity.HasKey(e => e.Id);

        // 配置购物车主键字段。
        entity.Property(e => e.Id)
            .HasColumnType("NUMBER(19)")
            .ValueGeneratedOnAdd();

        // 配置所属用户 ID：购物车必须归属于某个用户。
        entity.Property(e => e.UserId)
            .HasColumnType("NUMBER(19)")
            .IsRequired();

        // UserId 唯一索引：保证每个用户最多拥有一个购物车。
        entity.HasIndex(e => e.UserId)
            .IsUnique()
            .HasDatabaseName("UX_ShoppingCarts_UserId");

        // 配置购物车到用户的一对一关系。
        entity.HasOne(e => e.User)
            // 用户侧通过 ShoppingCart 导航属性访问自己的购物车。
            .WithOne(e => e.ShoppingCart)
            // ShoppingCart.UserId 作为外键引用 Users.Id。
            .HasForeignKey<ShoppingCart>(e => e.UserId)
            // Restrict 避免删除用户时自动级联删除购物车，便于按业务流程显式处理用户数据。
            .OnDelete(DeleteBehavior.Restrict);

        // 为购物车表统一配置 CreatedAt、UpdatedAt 审计字段。
        ConfigureAudit(entity);
    }

    /// <summary>
    /// 配置购物车明细表 CartItems，限制数量必须大于 0，并保证同一购物车中同一商品只出现一次。
    /// </summary>
    private static void ConfigureCartItem(EntityTypeBuilder<CartItem> entity)
    {
        // 将 CartItem 实体映射到 CartItems 表，并限制加购数量必须为正数。
        entity.ToTable("CartItems", table =>
        {
            // 数量检查约束：购物车中的商品数量必须大于 0。
            table.HasCheckConstraint("CK_CartItems_Quantity", "Quantity > 0");
        });

        // 指定 Id 为购物车明细表主键。
        entity.HasKey(e => e.Id);

        // 配置购物车明细主键字段。
        entity.Property(e => e.Id)
            .HasColumnType("NUMBER(19)")
            .ValueGeneratedOnAdd();

        // 配置所属购物车 ID。
        entity.Property(e => e.CartId)
            .HasColumnType("NUMBER(19)")
            .IsRequired();

        // 配置所属商品 ID。
        entity.Property(e => e.ProductId)
            .HasColumnType("NUMBER(19)")
            .IsRequired();

        // 配置加购数量，必须非空且由检查约束保证大于 0。
        entity.Property(e => e.Quantity)
            .HasColumnType("NUMBER(10)")
            .IsRequired();

        // 复合唯一索引：保证同一个购物车中同一个商品只存在一条明细，数量通过 Quantity 累加。
        entity.HasIndex(e => new { e.CartId, e.ProductId })
            .IsUnique()
            .HasDatabaseName("UX_CartItems_CartId_ProductId");

        // ProductId 索引用于快速定位包含某商品的购物车明细。
        entity.HasIndex(e => e.ProductId)
            .HasDatabaseName("IX_CartItems_ProductId");

        // 配置购物车明细到购物车的多对一关系。
        entity.HasOne(e => e.Cart)
            // 购物车侧通过 Items 导航属性访问多个商品项。
            .WithMany(e => e.Items)
            // CartId 外键引用 ShoppingCarts.Id。
            .HasForeignKey(e => e.CartId)
            // 删除购物车时级联删除其购物车明细，避免孤立明细记录。
            .OnDelete(DeleteBehavior.Cascade);

        // 配置购物车明细到商品的多对一关系。
        entity.HasOne(e => e.Product)
            // 商品侧通过 CartItems 导航属性关联所有购物车引用。
            .WithMany(e => e.CartItems)
            // ProductId 外键引用 Products.Id。
            .HasForeignKey(e => e.ProductId)
            // Restrict 防止删除商品时直接删除用户购物车明细，便于业务层先处理下架或失效商品。
            .OnDelete(DeleteBehavior.Restrict);

        // 为购物车明细表统一配置 CreatedAt、UpdatedAt 审计字段。
        ConfigureAudit(entity);
    }

    /// <summary>
    /// 配置订单表 Orders，包括订单编号唯一性、订单金额、订单状态、收货地址和支付超时时间。
    /// </summary>
    private static void ConfigureOrder(EntityTypeBuilder<Order> entity)
    {
        // 将 Order 实体映射到 Orders 表，并配置订单金额和订单状态检查约束。
        entity.ToTable("Orders", table =>
        {
            // 订单总金额不能为负。
            table.HasCheckConstraint("CK_Orders_TotalAmount", "TotalAmount >= 0");
            // 订单状态限制为 0=待支付、1=待发货、2=已发货、3=已完成、4=已取消。
            table.HasCheckConstraint("CK_Orders_Status", "Status IN (0, 1, 2, 3, 4)");
        });

        // 指定 Id 为订单表主键。
        entity.HasKey(e => e.Id);

        // 配置订单主键字段。
        entity.Property(e => e.Id)
            .HasColumnType("NUMBER(19)")
            .ValueGeneratedOnAdd();

        // 配置下单用户 ID，每个订单必须归属于一个用户。
        entity.Property(e => e.UserId)
            .HasColumnType("NUMBER(19)")
            .IsRequired();

        // 配置订单编号：面向业务和用户展示的唯一流水号。
        entity.Property(e => e.OrderNo)
            .HasColumnType("VARCHAR2(50)")
            .HasMaxLength(50)
            .IsRequired();

        // 配置订单总金额：使用 DECIMAL(18,2) 确保货币计算精度。
        entity.Property(e => e.TotalAmount)
            .HasColumnType("DECIMAL(18,2)")
            .HasPrecision(18, 2)
            .IsRequired();

        // 配置订单状态枚举，默认创建后为待支付状态。
        entity.Property(e => e.Status)
            .HasConversion<int>()
            .HasColumnType("NUMBER(10)")
            .HasDefaultValue(OrderStatus.PendingPayment)
            .IsRequired();

        // 配置收货地址：下单时必须填写，并作为订单快照保存。
        entity.Property(e => e.ShippingAddress)
            .HasColumnType("VARCHAR2(500)")
            .HasMaxLength(500)
            .IsRequired();

        // 配置订单备注：用于用户填写补充说明，可为空。
        entity.Property(e => e.Remark)
            .HasColumnType("VARCHAR2(500)")
            .HasMaxLength(500);

        // 配置支付超时时间：用于判断待支付订单是否应自动取消。
        entity.Property(e => e.ExpireAt)
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        // UserId 索引用于快速查询某个用户的订单列表。
        entity.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_Orders_UserId");

        // OrderNo 唯一索引保证业务订单号不重复，并提升按订单号查询效率。
        entity.HasIndex(e => e.OrderNo)
            .IsUnique()
            .HasDatabaseName("UX_Orders_OrderNo");

        // Status 索引用于后台按订单状态筛选待支付、待发货等订单。
        entity.HasIndex(e => e.Status)
            .HasDatabaseName("IX_Orders_Status");

        // 配置订单到用户的多对一关系。
        entity.HasOne(e => e.User)
            // 用户侧通过 Orders 导航属性访问自己的订单集合。
            .WithMany(e => e.Orders)
            // UserId 外键引用 Users.Id。
            .HasForeignKey(e => e.UserId)
            // Restrict 防止删除用户时级联删除历史订单，保证交易记录可追溯。
            .OnDelete(DeleteBehavior.Restrict);

        // 为订单表统一配置 CreatedAt、UpdatedAt 审计字段。
        ConfigureAudit(entity);
    }

    /// <summary>
    /// 配置订单明细表 OrderItems，保存商品数量、下单单价和小计金额，用于形成价格快照。
    /// </summary>
    private static void ConfigureOrderItem(EntityTypeBuilder<OrderItem> entity)
    {
        // 将 OrderItem 实体映射到 OrderItems 表，并配置数量、单价和小计金额的合法性约束。
        entity.ToTable("OrderItems", table =>
        {
            // 订单明细购买数量必须大于 0。
            table.HasCheckConstraint("CK_OrderItems_Quantity", "Quantity > 0");
            // 下单单价不能为负。
            table.HasCheckConstraint("CK_OrderItems_UnitPrice", "UnitPrice >= 0");
            // 小计金额不能为负。
            table.HasCheckConstraint("CK_OrderItems_SubTotal", "SubTotal >= 0");
        });

        // 指定 Id 为订单明细表主键。
        entity.HasKey(e => e.Id);

        // 配置订单明细主键字段。
        entity.Property(e => e.Id)
            .HasColumnType("NUMBER(19)")
            .ValueGeneratedOnAdd();

        // 配置所属订单 ID。
        entity.Property(e => e.OrderId)
            .HasColumnType("NUMBER(19)")
            .IsRequired();

        // 配置购买商品 ID。
        entity.Property(e => e.ProductId)
            .HasColumnType("NUMBER(19)")
            .IsRequired();

        // 配置购买数量。
        entity.Property(e => e.Quantity)
            .HasColumnType("NUMBER(10)")
            .IsRequired();

        // 配置下单时商品单价，作为价格快照保存，避免商品改价影响历史订单。
        entity.Property(e => e.UnitPrice)
            .HasColumnType("DECIMAL(18,2)")
            .HasPrecision(18, 2)
            .IsRequired();

        // 配置明细小计金额，通常等于 UnitPrice * Quantity。
        entity.Property(e => e.SubTotal)
            .HasColumnType("DECIMAL(18,2)")
            .HasPrecision(18, 2)
            .IsRequired();

        // OrderId 索引用于快速加载某个订单的全部明细。
        entity.HasIndex(e => e.OrderId)
            .HasDatabaseName("IX_OrderItems_OrderId");

        // ProductId 索引用于分析某商品相关订单明细。
        entity.HasIndex(e => e.ProductId)
            .HasDatabaseName("IX_OrderItems_ProductId");

        // 配置订单明细到订单的多对一关系。
        entity.HasOne(e => e.Order)
            // 订单侧通过 Items 导航属性访问多个明细项。
            .WithMany(e => e.Items)
            // OrderId 外键引用 Orders.Id。
            .HasForeignKey(e => e.OrderId)
            // 删除订单主记录时级联删除订单明细，保持主从表一致。
            .OnDelete(DeleteBehavior.Cascade);

        // 配置订单明细到商品的多对一关系。
        entity.HasOne(e => e.Product)
            // 商品侧通过 OrderItems 导航属性关联历史订单明细。
            .WithMany(e => e.OrderItems)
            // ProductId 外键引用 Products.Id。
            .HasForeignKey(e => e.ProductId)
            // Restrict 防止删除商品时破坏历史订单明细的可追溯性。
            .OnDelete(DeleteBehavior.Restrict);

        // 为订单明细表统一配置 CreatedAt、UpdatedAt 审计字段。
        ConfigureAudit(entity);
    }

    /// <summary>
    /// 配置支付记录表 Payments，包括订单一对一支付记录、支付金额、支付方式、支付状态和第三方流水号。
    /// </summary>
    private static void ConfigurePayment(EntityTypeBuilder<Payment> entity)
    {
        // 将 Payment 实体映射到 Payments 表，并配置金额、支付方式和支付状态检查约束。
        entity.ToTable("Payments", table =>
        {
            // 实际支付金额不能为负。
            table.HasCheckConstraint("CK_Payments_Amount", "Amount >= 0");
            // 支付方式限制为 0=支付宝、1=微信、2=信用卡。
            table.HasCheckConstraint("CK_Payments_PaymentMethod", "PaymentMethod IN (0, 1, 2)");
            // 支付状态限制为 0=待支付、1=支付成功、2=支付失败。
            table.HasCheckConstraint("CK_Payments_Status", "Status IN (0, 1, 2)");
        });

        // 指定 Id 为支付记录表主键。
        entity.HasKey(e => e.Id);

        // 配置支付记录主键字段。
        entity.Property(e => e.Id)
            .HasColumnType("NUMBER(19)")
            .ValueGeneratedOnAdd();

        // 配置关联订单 ID，每条支付记录必须对应一个订单。
        entity.Property(e => e.OrderId)
            .HasColumnType("NUMBER(19)")
            .IsRequired();

        // 配置实际支付金额，使用 DECIMAL(18,2) 确保货币精度。
        entity.Property(e => e.Amount)
            .HasColumnType("DECIMAL(18,2)")
            .HasPrecision(18, 2)
            .IsRequired();

        // 配置支付方式枚举，以整数形式落库。
        entity.Property(e => e.PaymentMethod)
            .HasConversion<int>()
            .HasColumnType("NUMBER(10)")
            .IsRequired();

        // 配置支付状态枚举，默认值为待支付。
        entity.Property(e => e.Status)
            .HasConversion<int>()
            .HasColumnType("NUMBER(10)")
            .HasDefaultValue(PaymentStatus.Pending)
            .IsRequired();

        // 配置第三方交易流水号，用于与支付宝、微信或银行支付系统对账。
        entity.Property(e => e.TransactionId)
            .HasColumnType("VARCHAR2(100)")
            .HasMaxLength(100);

        // 配置支付完成时间：待支付或失败时可为空。
        entity.Property(e => e.PaidAt)
            .HasColumnType("TIMESTAMP");

        // OrderId 唯一索引：保证一个订单最多对应一条支付记录。
        entity.HasIndex(e => e.OrderId)
            .IsUnique()
            .HasDatabaseName("UX_Payments_OrderId");

        // TransactionId 索引用于根据第三方流水号快速查询支付记录。
        entity.HasIndex(e => e.TransactionId)
            .HasDatabaseName("IX_Payments_TransactionId");

        // 配置支付记录到订单的一对一关系。
        entity.HasOne(e => e.Order)
            // 订单侧通过 Payment 导航属性访问其支付记录。
            .WithOne(e => e.Payment)
            // Payment.OrderId 作为外键引用 Orders.Id。
            .HasForeignKey<Payment>(e => e.OrderId)
            // 删除订单时级联删除对应支付记录，保持订单与支付主从关系一致。
            .OnDelete(DeleteBehavior.Cascade);

        // 为支付记录表统一配置 CreatedAt、UpdatedAt 审计字段。
        ConfigureAudit(entity);
    }

    /// <summary>
    /// 配置商品评价表 ProductReviews，限制评分范围，并防止同一用户对同一订单商品重复评价。
    /// </summary>
    private static void ConfigureProductReview(EntityTypeBuilder<ProductReview> entity)
    {
        // 将 ProductReview 实体映射到 ProductReviews 表，并限制评分必须在 1 到 5 星之间。
        entity.ToTable("ProductReviews", table =>
        {
            // 评分检查约束：评价分值只能是 1 到 5。
            table.HasCheckConstraint("CK_ProductReviews_Rating", "Rating >= 1 AND Rating <= 5");
        });

        // 指定 Id 为商品评价表主键。
        entity.HasKey(e => e.Id);

        // 配置评价记录主键字段。
        entity.Property(e => e.Id)
            .HasColumnType("NUMBER(19)")
            .ValueGeneratedOnAdd();

        // 配置被评价商品 ID。
        entity.Property(e => e.ProductId)
            .HasColumnType("NUMBER(19)")
            .IsRequired();

        // 配置评价用户 ID。
        entity.Property(e => e.UserId)
            .HasColumnType("NUMBER(19)")
            .IsRequired();

        // 配置关联订单 ID，用于确认评价来自具体购买记录。
        entity.Property(e => e.OrderId)
            .HasColumnType("NUMBER(19)")
            .IsRequired();

        // 配置评分字段，非空并由检查约束限制范围。
        entity.Property(e => e.Rating)
            .HasColumnType("NUMBER(10)")
            .IsRequired();

        // 配置文字评价内容，可为空，最大 1000 字符。
        entity.Property(e => e.Comment)
            .HasColumnType("VARCHAR2(1000)")
            .HasMaxLength(1000);

        // ProductId 索引用于查询某个商品的评价列表。
        entity.HasIndex(e => e.ProductId)
            .HasDatabaseName("IX_ProductReviews_ProductId");

        // UserId 索引用于查询某个用户发表过的评价。
        entity.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_ProductReviews_UserId");

        // OrderId 索引用于根据订单追踪评价记录。
        entity.HasIndex(e => e.OrderId)
            .HasDatabaseName("IX_ProductReviews_OrderId");

        // 业务上通常不允许同一用户对同一订单中的同一商品重复评价。
        // 复合唯一索引：限制同一用户不能对同一订单中的同一商品重复评价。
        entity.HasIndex(e => new { e.UserId, e.OrderId, e.ProductId })
            .IsUnique()
            .HasDatabaseName("UX_ProductReviews_User_Order_Product");

        // 配置评价到商品的多对一关系。
        entity.HasOne(e => e.Product)
            // 商品侧通过 Reviews 导航属性访问全部评价。
            // 订单侧通过 Reviews 导航属性访问该订单产生的评价。
            .WithMany(e => e.Reviews)
            // ProductId 外键引用 Products.Id。
            .HasForeignKey(e => e.ProductId)
            // Restrict 防止删除商品、用户或订单时破坏评价和交易记录的关联完整性。
            .OnDelete(DeleteBehavior.Restrict);

        // 配置评价到用户的多对一关系。
        entity.HasOne(e => e.User)
            // 用户侧通过 ProductReviews 导航属性访问其发表的评价。
            .WithMany(e => e.ProductReviews)
            // UserId 外键引用 Users.Id。
            .HasForeignKey(e => e.UserId)
            // Restrict 防止删除商品、用户或订单时破坏评价和交易记录的关联完整性。
            .OnDelete(DeleteBehavior.Restrict);

        // 配置评价到订单的多对一关系。
        entity.HasOne(e => e.Order)
            // 商品侧通过 Reviews 导航属性访问全部评价。
            // 订单侧通过 Reviews 导航属性访问该订单产生的评价。
            .WithMany(e => e.Reviews)
            // OrderId 外键引用 Orders.Id。
            .HasForeignKey(e => e.OrderId)
            // Restrict 防止删除商品、用户或订单时破坏评价和交易记录的关联完整性。
            .OnDelete(DeleteBehavior.Restrict);

        // 为商品评价表统一配置 CreatedAt、UpdatedAt 审计字段。
        ConfigureAudit(entity);
    }

    /// <summary>
    /// 配置客服工单表 CustomerServiceTickets，包括提交人、关联订单、分配客服和工单状态等关系。
    /// </summary>
    private static void ConfigureCustomerServiceTicket(EntityTypeBuilder<CustomerServiceTicket> entity)
    {
        // 将 CustomerServiceTicket 实体映射到 CustomerServiceTickets 表，并限制工单状态合法范围。
        entity.ToTable("CustomerServiceTickets", table =>
        {
            // 工单状态限制为 0=待处理、1=处理中、2=已解决、3=已关闭。
            table.HasCheckConstraint("CK_CustomerServiceTickets_Status", "Status IN (0, 1, 2, 3)");
        });

        // 指定 Id 为客服工单表主键。
        entity.HasKey(e => e.Id);

        // 配置工单主键字段。
        entity.Property(e => e.Id)
            .HasColumnType("NUMBER(19)")
            .ValueGeneratedOnAdd();

        // 配置提交用户 ID：每个工单必须有提交人。
        entity.Property(e => e.UserId)
            .HasColumnType("NUMBER(19)")
            .IsRequired();

        // 配置关联订单 ID：售后问题可能关联订单，也可能为空。
        entity.Property(e => e.OrderId)
            .HasColumnType("NUMBER(19)");

        // 配置分配客服 ID：未分配时可以为空。
        entity.Property(e => e.AssignedTo)
            .HasColumnType("NUMBER(19)");

        // 配置工单主题：用于概括问题，不能为空。
        entity.Property(e => e.Subject)
            .HasColumnType("VARCHAR2(200)")
            .HasMaxLength(200)
            .IsRequired();

        // 配置问题描述：记录用户提交的详细问题内容，不能为空。
        entity.Property(e => e.Description)
            .HasColumnType("VARCHAR2(2000)")
            .HasMaxLength(2000)
            .IsRequired();

        // 配置客服回复：客服处理后填写，可为空。
        entity.Property(e => e.Reply)
            .HasColumnType("VARCHAR2(2000)")
            .HasMaxLength(2000);

        // 配置工单状态枚举，默认值为待处理。
        entity.Property(e => e.Status)
            .HasConversion<int>()
            .HasColumnType("NUMBER(10)")
            .HasDefaultValue(TicketStatus.Pending)
            .IsRequired();

        // UserId 索引用于查询某个用户提交的工单。
        entity.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_CustomerServiceTickets_UserId");

        // OrderId 索引用于查询某个订单关联的售后工单。
        entity.HasIndex(e => e.OrderId)
            .HasDatabaseName("IX_CustomerServiceTickets_OrderId");

        // AssignedTo 索引用于查询分配给某个客服的工单。
        entity.HasIndex(e => e.AssignedTo)
            .HasDatabaseName("IX_CustomerServiceTickets_AssignedTo");

        // Status 索引用于后台按待处理、处理中等状态筛选工单。
        entity.HasIndex(e => e.Status)
            .HasDatabaseName("IX_CustomerServiceTickets_Status");

        // 提交人关系：CustomerServiceTickets.UserId -> Users.Id。
        // 配置工单提交人与用户的一对多关系。
        entity.HasOne(e => e.User)
            // 用户侧通过 SubmittedTickets 导航属性访问其提交的工单。
            .WithMany(e => e.SubmittedTickets)
            // UserId 外键引用 Users.Id。
            .HasForeignKey(e => e.UserId)
            // Restrict 防止删除用户时级联删除历史工单，保留售后记录。
            .OnDelete(DeleteBehavior.Restrict);

        // 订单关系：CustomerServiceTickets.OrderId -> Orders.Id，可空。
        // 配置工单与订单的可选多对一关系。
        entity.HasOne(e => e.Order)
            // 订单侧通过 Tickets 导航属性访问关联售后工单。
            .WithMany(e => e.Tickets)
            // OrderId 外键引用 Orders.Id，允许为空。
            .HasForeignKey(e => e.OrderId)
            // SetNull 表示订单被删除时仅清空关联订单 ID，不删除工单本身。
            .OnDelete(DeleteBehavior.SetNull);

        // 分配客服关系：CustomerServiceTickets.AssignedTo -> Users.Id，可空。
        // 配置工单与被分配客服用户的可选多对一关系。
        entity.HasOne(e => e.AssignedCustomerService)
            // 客服用户侧通过 AssignedTickets 导航属性访问分配给自己的工单。
            .WithMany(e => e.AssignedTickets)
            // AssignedTo 外键引用 Users.Id，允许为空。
            .HasForeignKey(e => e.AssignedTo)
            // SetNull 表示订单被删除时仅清空关联订单 ID，不删除工单本身。
            .OnDelete(DeleteBehavior.SetNull);

        // 为客服工单表统一配置 CreatedAt、UpdatedAt 审计字段。
        ConfigureAudit(entity);
    }
}
