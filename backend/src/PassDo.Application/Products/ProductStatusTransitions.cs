using PassDo.Domain.Enums;

namespace PassDo.Application.Products;

public static class ProductStatusTransitions
{
    public static bool IsSystemManaged(ProductStatus status) =>
        status is ProductStatus.Reserved or ProductStatus.Sold;

    public static bool IsPubliclyListable(ProductStatus status) =>
        status == ProductStatus.Active;

    public static bool CanSellerTransition(ProductStatus from, ProductStatus to) =>
        (from, to) switch
        {
            (ProductStatus.Draft, ProductStatus.Draft) => true,
            (ProductStatus.Draft, ProductStatus.PendingReview) => true,
            (ProductStatus.PendingReview, ProductStatus.Draft) => true,
            (ProductStatus.Rejected, ProductStatus.Draft) => true,
            (ProductStatus.Active, ProductStatus.Hidden) => true,
            (ProductStatus.Hidden, ProductStatus.Active) => true,
            _ => false
        };

    public static bool CanAdminTransition(ProductStatus from, ProductStatus to) =>
        (from, to) switch
        {
            (ProductStatus.PendingReview, ProductStatus.Active) => true,
            (ProductStatus.PendingReview, ProductStatus.Rejected) => true,
            _ => false
        };
}
