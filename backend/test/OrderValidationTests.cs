using RetailSystem.Api.Services;

namespace RetailSystem.Api.Tests;

public sealed class OrderValidationTests
{
    [Fact]
    public void Valid_Order_Request_Is_Accepted()
    {
        var error = OrderValidation.Create(
            [1, 2, 3],
            "上海市杨浦区四平路 1239 号",
            "请尽快发货");

        Assert.Null(error);
    }

    [Fact]
    public void Empty_Cart_Item_List_Is_Rejected()
    {
        var error = OrderValidation.Create(
            [],
            "测试地址",
            null);

        Assert.Equal(
            "至少选择一件购物车商品",
            error);
    }

    [Fact]
    public void Too_Many_Cart_Items_Are_Rejected()
    {
        var ids = Enumerable
            .Range(1, OrderValidation.MaxItemTypes + 1)
            .Select(id => (long)id)
            .ToArray();

        var error = OrderValidation.Create(
            ids,
            "测试地址",
            null);

        Assert.Equal(
            $"单次结算不能超过 {OrderValidation.MaxItemTypes} 种商品",
            error);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void Invalid_Cart_Item_Id_Is_Rejected(
        long cartItemId)
    {
        var error = OrderValidation.Create(
            [cartItemId],
            "测试地址",
            null);

        Assert.Equal(
            "购物车商品编号无效",
            error);
    }

    [Fact]
    public void Duplicate_Cart_Items_Are_Rejected()
    {
        var error = OrderValidation.Create(
            [1, 1],
            "测试地址",
            null);

        Assert.Equal(
            "购物车商品不能重复选择",
            error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_Shipping_Address_Is_Rejected(
        string address)
    {
        var error = OrderValidation.Create(
            [1],
            address,
            null);

        Assert.Equal(
            $"收货地址不能为空且不能超过 {OrderValidation.MaxShippingAddressLength} 个字符",
            error);
    }

    [Fact]
    public void Overlong_Shipping_Address_Is_Rejected()
    {
        var error = OrderValidation.Create(
            [1],
            new string(
                'A',
                OrderValidation.MaxShippingAddressLength + 1),
            null);

        Assert.Equal(
            $"收货地址不能为空且不能超过 {OrderValidation.MaxShippingAddressLength} 个字符",
            error);
    }

    [Fact]
    public void Overlong_Remark_Is_Rejected()
    {
        var error = OrderValidation.Create(
            [1],
            "测试地址",
            new string(
                'A',
                OrderValidation.MaxRemarkLength + 1));

        Assert.Equal(
            $"订单备注不能超过 {OrderValidation.MaxRemarkLength} 个字符",
            error);
    }

    [Fact]
    public void Maximum_Boundary_Values_Are_Accepted()
    {
        var ids = Enumerable
            .Range(1, OrderValidation.MaxItemTypes)
            .Select(id => (long)id)
            .ToArray();

        var error = OrderValidation.Create(
            ids,
            new string(
                'A',
                OrderValidation.MaxShippingAddressLength),
            new string(
                'B',
                OrderValidation.MaxRemarkLength));

        Assert.Null(error);
    }
}