namespace TariffPaymentAccounting.Core.Models;

public sealed class AccountingSnapshot
{
    public List<TariffPlan> Tariffs { get; set; } = [];

    public List<Customer> Customers { get; set; } = [];

    public List<Payment> Payments { get; set; } = [];
}
