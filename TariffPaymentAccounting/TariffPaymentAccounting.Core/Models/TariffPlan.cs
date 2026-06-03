namespace TariffPaymentAccounting.Core.Models;

public sealed class TariffPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal MonthlyFee { get; set; }

    public bool IsArchived { get; set; }
}
