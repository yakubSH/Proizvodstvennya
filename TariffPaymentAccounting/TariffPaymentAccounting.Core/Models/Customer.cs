namespace TariffPaymentAccounting.Core.Models;

public sealed class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string AccountNumber { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public Guid TariffPlanId { get; set; }

    public bool IsActive { get; set; } = true;
}
