namespace PassDo.Domain.Enums;

public enum OrderRejectReason
{
    OutOfStock = 0,
    SoldElsewhere = 1,
    CannotDeliver = 2,
    WrongPrice = 3,
    Other = 4
}
