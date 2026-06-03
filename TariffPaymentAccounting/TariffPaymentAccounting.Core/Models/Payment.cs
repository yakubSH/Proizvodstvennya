namespace TariffPaymentAccounting.Core.Models;

public sealed class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CustomerId { get; set; }

    public Guid TariffPlanId { get; set; }

    public DateTime Period { get; set; }

    public DateTime PaidAt { get; set; } = DateTime.Now;

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    public string Note { get; set; } = string.Empty;
}
