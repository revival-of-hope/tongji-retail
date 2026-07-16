using RetailSystem.Api.Contracts;
using RetailSystem.Api.Models;

namespace RetailSystem.Api.Services;

public static class MappingExtensions
{
    public static MerchantSummary ToSummary(this Merchant merchant) => new(
        merchant.Id,
        merchant.UserId,
        merchant.StoreName,
        merchant.Description,
        merchant.Status,
        merchant.CreatedAt);

    public static UserSummary ToSummary(this User user) => new(
        user.Id,
        user.Username,
        user.Email,
        user.Phone,
        user.Role,
        user.IsActive,
        user.CreatedAt,
        user.Merchant?.ToSummary());

    public static ProductListItem ToListItem(this Product product) => new(
        product.Id,
        product.Name,
        product.Price,
        product.StockQuantity,
        product.SoldCount,
        product.AvgRating,
        product.ReviewCount,
        product.Status,
        product.Merchant.StoreName,
        product.Category.Name,
        product.Images.OrderByDescending(x => x.IsMain).ThenBy(x => x.SortOrder).Select(x => x.ImageUrl).FirstOrDefault(),
        product.CreatedAt);

    public static ProductDetail ToDetail(this Product product) => new(
        product.Id,
        product.MerchantId,
        product.Merchant.StoreName,
        product.CategoryId,
        product.Category.Name,
        product.Name,
        product.Description,
        product.Price,
        product.StockQuantity,
        product.SoldCount,
        product.AvgRating,
        product.ReviewCount,
        product.Status,
        product.Images.OrderByDescending(x => x.IsMain).ThenBy(x => x.SortOrder).Select(x => x.ImageUrl).ToArray(),
        product.CreatedAt,
        product.UpdatedAt);

    public static CartItemResponse ToResponse(this CartItem item) => new(
        item.Id,
        item.ProductId,
        item.Product.Name,
        item.Product.Merchant.StoreName,
        item.Product.Price,
        item.Quantity,
        item.Product.StockQuantity,
        item.Product.Images.OrderByDescending(x => x.IsMain).ThenBy(x => x.SortOrder).Select(x => x.ImageUrl).FirstOrDefault(),
        item.Product.Status == ProductStatus.OnSale && item.Product.StockQuantity >= item.Quantity);

    public static OrderItemResponse ToResponse(this OrderItem item) => new(
        item.Id,
        item.ProductId,
        item.Product.Name,
        item.Product.Merchant.StoreName,
        item.Quantity,
        item.UnitPrice,
        item.SubTotal,
        item.Product.Images.OrderByDescending(x => x.IsMain).ThenBy(x => x.SortOrder).Select(x => x.ImageUrl).FirstOrDefault());

    public static PaymentResponse ToResponse(this Payment payment) => new(
        payment.Id,
        payment.Amount,
        payment.PaymentMethod,
        payment.Status,
        payment.TransactionId,
        payment.PaidAt);

    public static OrderSummary ToSummary(this Order order) => new(
        order.Id,
        order.OrderNo,
        order.TotalAmount,
        order.Status,
        order.ShippingAddress,
        order.ExpireAt,
        order.CreatedAt,
        order.Items.Select(x => x.ToResponse()).ToArray());

    public static OrderDetail ToDetail(this Order order) => new(
        order.Id,
        order.OrderNo,
        order.UserId,
        order.User.Username,
        order.TotalAmount,
        order.Status,
        order.ShippingAddress,
        order.Remark,
        order.ExpireAt,
        order.CreatedAt,
        order.UpdatedAt,
        order.Items.Select(x => x.ToResponse()).ToArray(),
        order.Payment?.ToResponse());

    public static ProductReviewResponse ToResponse(this ProductReview review) => new(
        review.Id,
        review.ProductId,
        review.OrderId,
        review.User.Username,
        review.Rating,
        review.Comment,
        review.CreatedAt);

    public static TicketResponse ToResponse(this CustomerServiceTicket ticket) => new(
        ticket.Id,
        ticket.UserId,
        ticket.User.Username,
        ticket.OrderId,
        ticket.AssignedTo,
        ticket.AssignedUser?.Username,
        ticket.Subject,
        ticket.Description,
        ticket.Reply,
        ticket.Status,
        ticket.CreatedAt,
        ticket.UpdatedAt);
}
