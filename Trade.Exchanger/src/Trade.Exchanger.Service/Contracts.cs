namespace Trade.Exchanger.Service.Contracts;

public record PurchaseRequested(
    Guid UserId,
    Guid ItemId,
    int Quantity,
    Guid CorrelationId);