namespace TariffPaymentAccounting.Core.Models;

public sealed record MonthlyCharge(
    Guid CustomerId,
    string AccountNumber,
    string CustomerName,
    string TariffName,
    DateTime Period,
    decimal RequiredAmount,
    decimal PaidAmount,
    decimal Balance,
    PaymentStatus Status);
