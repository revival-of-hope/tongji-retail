namespace RetailSystem.Api.Services;

public static class OrderValidation
{
    public const int MaxItemTypes = 100;
    public const int MaxShippingAddressLength = 500;
    public const int MaxRemarkLength = 500;

    public static string? Create(
        IReadOnlyList<long>? cartItemIds,
        string? shippingAddress,
        string? remark)
    {
        if (cartItemIds is null || cartItemIds.Count == 0)
            return "至少选择一件购物车商品";

        if (cartItemIds.Count > MaxItemTypes)
            return $"单次结算不能超过 {MaxItemTypes} 种商品";

        if (string.IsNullOrWhiteSpace(shippingAddress) ||
            shippingAddress.Trim().Length > MaxShippingAddressLength)
        {
            return $"收货地址不能为空且不能超过 {MaxShippingAddressLength} 个字符";
        }

        if (remark?.Trim().Length > MaxRemarkLength)
            return $"订单备注不能超过 {MaxRemarkLength} 个字符";

        if (cartItemIds.Any(id => id <= 0))
            return "购物车商品编号无效";

        if (cartItemIds.Distinct().Count() != cartItemIds.Count)
            return "购物车商品不能重复选择";

        return null;
    }
}