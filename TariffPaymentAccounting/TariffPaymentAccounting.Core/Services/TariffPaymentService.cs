using TariffPaymentAccounting.Core.Models;

namespace TariffPaymentAccounting.Core.Services;

public sealed class TariffPaymentService
{
    private readonly AccountingSnapshot _snapshot;

    public TariffPaymentService(AccountingSnapshot snapshot)
    {
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    public AccountingSnapshot Snapshot => _snapshot;

    public IReadOnlyList<TariffPlan> Tariffs => _snapshot.Tariffs
        .OrderBy(tariff => tariff.IsArchived)
        .ThenBy(tariff => tariff.Name)
        .ToArray();

    public IReadOnlyList<Customer> Customers => _snapshot.Customers
        .OrderByDescending(customer => customer.IsActive)
        .ThenBy(customer => customer.FullName)
        .ToArray();

    public IReadOnlyList<Payment> Payments => _snapshot.Payments
        .OrderByDescending(payment => payment.PaidAt)
        .ThenByDescending(payment => payment.Period)
        .ToArray();

    public Customer AddCustomer(string accountNumber, string fullName, string phone, string address, Guid tariffPlanId)
    {
        var customer = new Customer
        {
            AccountNumber = NormalizeAccountNumber(accountNumber),
            FullName = Required(fullName, "ФИО клиента"),
            Phone = phone.Trim(),
            Address = address.Trim(),
            TariffPlanId = RequireTariff(tariffPlanId).Id
        };

        EnsureAccountNumberIsUnique(customer.AccountNumber, null);
        _snapshot.Customers.Add(customer);
        return customer;
    }

    public void UpdateCustomer(Guid id, string accountNumber, string fullName, string phone, string address, Guid tariffPlanId, bool isActive)
    {
        var customer = RequireCustomer(id);
        var normalizedAccount = NormalizeAccountNumber(accountNumber);

        EnsureAccountNumberIsUnique(normalizedAccount, id);
        customer.AccountNumber = normalizedAccount;
        customer.FullName = Required(fullName, "ФИО клиента");
        customer.Phone = phone.Trim();
        customer.Address = address.Trim();
        customer.TariffPlanId = RequireTariff(tariffPlanId).Id;
        customer.IsActive = isActive;
    }

    public void DeactivateCustomer(Guid id)
    {
        RequireCustomer(id).IsActive = false;
    }

    public TariffPlan AddTariff(string name, decimal monthlyFee, string description)
    {
        var tariff = new TariffPlan
        {
            Name = Required(name, "Название тарифа"),
            MonthlyFee = RequirePositiveAmount(monthlyFee, "Абонентская плата"),
            Description = description.Trim()
        };

        EnsureTariffNameIsUnique(tariff.Name, null);
        _snapshot.Tariffs.Add(tariff);
        return tariff;
    }

    public void UpdateTariff(Guid id, string name, decimal monthlyFee, string description, bool isArchived)
    {
        var tariff = RequireTariff(id);
        var normalizedName = Required(name, "Название тарифа");

        EnsureTariffNameIsUnique(normalizedName, id);
        tariff.Name = normalizedName;
        tariff.MonthlyFee = RequirePositiveAmount(monthlyFee, "Абонентская плата");
        tariff.Description = description.Trim();
        tariff.IsArchived = isArchived;
    }

    public void ArchiveTariff(Guid id)
    {
        RequireTariff(id).IsArchived = true;
    }

    public Payment AddPayment(Guid customerId, DateTime period, decimal amount, PaymentMethod method, string note)
    {
        var customer = RequireCustomer(customerId);
        if (!customer.IsActive)
        {
            throw new InvalidOperationException("Нельзя принять оплату от неактивного клиента.");
        }

        var tariff = RequireTariff(customer.TariffPlanId);
        var payment = new Payment
        {
            CustomerId = customer.Id,
            TariffPlanId = tariff.Id,
            Period = NormalizePeriod(period),
            Amount = RequirePositiveAmount(amount, "Сумма оплаты"),
            Method = method,
            Note = note.Trim()
        };

        _snapshot.Payments.Add(payment);
        return payment;
    }

    public void RemovePayment(Guid id)
    {
        var payment = _snapshot.Payments.SingleOrDefault(item => item.Id == id)
            ?? throw new InvalidOperationException("Платеж не найден.");

        _snapshot.Payments.Remove(payment);
    }

    public IReadOnlyList<MonthlyCharge> BuildMonthlyReport(DateTime period)
    {
        var normalizedPeriod = NormalizePeriod(period);

        return _snapshot.Customers
            .Where(customer => customer.IsActive)
            .OrderBy(customer => customer.FullName)
            .Select(customer =>
            {
                var tariff = RequireTariff(customer.TariffPlanId);
                var paid = _snapshot.Payments
                    .Where(payment => payment.CustomerId == customer.Id && NormalizePeriod(payment.Period) == normalizedPeriod)
                    .Sum(payment => payment.Amount);
                var balance = paid - tariff.MonthlyFee;

                return new MonthlyCharge(
                    customer.Id,
                    customer.AccountNumber,
                    customer.FullName,
                    tariff.Name,
                    normalizedPeriod,
                    tariff.MonthlyFee,
                    paid,
                    balance,
                    GetStatus(tariff.MonthlyFee, paid));
            })
            .ToArray();
    }

    public Customer GetCustomer(Guid id) => RequireCustomer(id);

    public TariffPlan GetTariff(Guid id) => RequireTariff(id);

    public string GetTariffName(Guid tariffPlanId) => RequireTariff(tariffPlanId).Name;

    public string GetCustomerName(Guid customerId) => RequireCustomer(customerId).FullName;

    private Customer RequireCustomer(Guid id) => _snapshot.Customers.SingleOrDefault(customer => customer.Id == id)
        ?? throw new InvalidOperationException("Клиент не найден.");

    private TariffPlan RequireTariff(Guid id) => _snapshot.Tariffs.SingleOrDefault(tariff => tariff.Id == id)
        ?? throw new InvalidOperationException("Тариф не найден.");

    private static DateTime NormalizePeriod(DateTime period) => new(period.Year, period.Month, 1);

    private string NormalizeAccountNumber(string accountNumber)
    {
        var normalized = accountNumber.Trim();
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            return normalized;
        }

        var nextNumber = _snapshot.Customers
            .Select(customer => int.TryParse(customer.AccountNumber, out var number) ? number : 100000)
            .DefaultIfEmpty(100000)
            .Max() + 1;

        return nextNumber.ToString();
    }

    private static string Required(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Поле \"{fieldName}\" обязательно для заполнения.");
        }

        return value.Trim();
    }

    private static decimal RequirePositiveAmount(decimal amount, string fieldName)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException($"Поле \"{fieldName}\" должно быть больше 0.");
        }

        return amount;
    }

    private void EnsureAccountNumberIsUnique(string accountNumber, Guid? currentCustomerId)
    {
        var exists = _snapshot.Customers.Any(customer =>
            customer.Id != currentCustomerId &&
            string.Equals(customer.AccountNumber, accountNumber, StringComparison.OrdinalIgnoreCase));

        if (exists)
        {
            throw new InvalidOperationException("Лицевой счет уже используется другим клиентом.");
        }
    }

    private void EnsureTariffNameIsUnique(string name, Guid? currentTariffId)
    {
        var exists = _snapshot.Tariffs.Any(tariff =>
            tariff.Id != currentTariffId &&
            string.Equals(tariff.Name, name, StringComparison.OrdinalIgnoreCase));

        if (exists)
        {
            throw new InvalidOperationException("Тариф с таким названием уже существует.");
        }
    }

    private static PaymentStatus GetStatus(decimal required, decimal paid)
    {
        if (paid == 0)
        {
            return PaymentStatus.Unpaid;
        }

        if (paid < required)
        {
            return PaymentStatus.Partial;
        }

        return paid == required ? PaymentStatus.Paid : PaymentStatus.Overpaid;
    }
}
