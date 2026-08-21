using System.Net.Mail;

namespace RetailSystem.Api.Services;

public static class Validation
{
    public const decimal MaxMoney = 9_999_999_999_999_999.99m;
    public const int MaxProductDescriptionLength = 5_000;
    public const int MaxImageCount = 8;
    public const int MaxImageUrlLength = 500;
    public const int MaxReviewCommentLength = 1_000;

    private static readonly HashSet<string> ProductSortOptions = new(StringComparer.OrdinalIgnoreCase)
    {
        "newest",
        "sales",
        "rating",
        "price_asc",
        "price_desc"
    };

    public static string? Register(string? username, string? password, string? email, string? phone)
    {
        username = username?.Trim();
        if (string.IsNullOrEmpty(username) || username.Length is < 3 or > 50)
            return "用户名长度必须为 3—50 个字符";
        if (string.IsNullOrEmpty(password) || password.Length is < 6 or > 100)
            return "密码长度必须为 6—100 个字符";

        if (!string.IsNullOrWhiteSpace(email))
        {
            email = email.Trim();
            if (email.Length > 100) return "邮箱不能超过 100 个字符";
            if (!MailAddress.TryCreate(email, out _)) return "邮箱格式不正确";
        }

        if (!string.IsNullOrWhiteSpace(phone) && phone.Trim().Length > 20)
            return "手机号不能超过 20 个字符";

        return null;
    }

    public static string? Login(string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            return "用户名和密码不能为空";
        if (username.Trim().Length > 50 || password.Length > 100)
            return "用户名或密码格式不正确";
        return null;
    }

    public static string? Product(
        string? name,
        string? description,
        decimal price,
        int stock,
        IReadOnlyList<string>? imageUrls)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200)
            return "商品名称不能为空且不能超过 200 个字符";
        if (description?.Trim().Length > MaxProductDescriptionLength)
            return $"商品描述不能超过 {MaxProductDescriptionLength} 个字符";
        if (price <= 0 || price > MaxMoney)
            return $"商品价格必须大于 0 且不能超过 {MaxMoney:0.00}";
        if (stock < 0) return "库存不能小于 0";

        var urls = imageUrls ?? [];
        if (urls.Count > MaxImageCount) return $"商品图片不能超过 {MaxImageCount} 张";
        foreach (var rawUrl in urls)
        {
            if (string.IsNullOrWhiteSpace(rawUrl)) continue;
            var url = rawUrl.Trim();
            if (url.Length > MaxImageUrlLength)
                return $"图片 URL 不能超过 {MaxImageUrlLength} 个字符";
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                return "图片 URL 必须是有效的 HTTPS 地址";
        }

        return null;
    }

    public static string? ProductQuery(
        int pageIndex,
        int pageSize,
        string? keyword,
        decimal? minPrice,
        decimal? maxPrice,
        string? sortBy)
    {
        if (pageIndex is < 1 or > 1_000_000) return "页码必须为 1—1000000";
        if (pageSize is < 1 or > 100) return "每页数量必须为 1—100";
        if (keyword?.Trim().Length > 200) return "搜索关键词不能超过 200 个字符";
        if (minPrice is < 0 || minPrice > MaxMoney) return "最低价格超出有效范围";
        if (maxPrice is < 0 || maxPrice > MaxMoney) return "最高价格超出有效范围";
        if (minPrice.HasValue && maxPrice.HasValue && minPrice.Value > maxPrice.Value)
            return "最低价格不能高于最高价格";
        if (string.IsNullOrWhiteSpace(sortBy) || !ProductSortOptions.Contains(sortBy))
            return "排序方式无效";
        return null;
    }

    public static string? ProductReview(
        long productId,
        long orderId,
        int rating,
        string? comment)
    {
        if (productId <= 0)
            return "商品编号无效";

        if (orderId <= 0)
            return "订单编号无效";

        if (rating is < 1 or > 5)
            return "评分必须为 1—5 星";

        if (comment?.Trim().Length > MaxReviewCommentLength)
            return $"评价内容不能超过 {MaxReviewCommentLength} 个字符";

        return null;
    }

    public static string? Ticket(string? subject, string? description)
    {
        if (string.IsNullOrWhiteSpace(subject) || subject.Trim().Length > 200)
            return "工单主题不能为空且不能超过 200 个字符";
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > 2000)
            return "工单描述不能为空且不能超过 2000 个字符";
        return null;
    }
}
