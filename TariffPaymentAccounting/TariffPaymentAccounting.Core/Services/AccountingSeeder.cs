using TariffPaymentAccounting.Core.Models;

namespace TariffPaymentAccounting.Core.Services;

public static class AccountingSeeder
{
    public static void SeedIfEmpty(AccountingSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.Tariffs.Count == 0)
        {
            snapshot.Tariffs.AddRange(
            [
                new TariffPlan
                {
                    Name = "Базовый",
                    Description = "Минимальный тариф для физических лиц",
                    MonthlyFee = 450m
                },
                new TariffPlan
                {
                    Name = "Стандарт",
                    Description = "Основной тариф с расширенным набором услуг",
                    MonthlyFee = 750m
                },
                new TariffPlan
                {
                    Name = "Премиум",
                    Description = "Тариф для клиентов с повышенным объемом услуг",
                    MonthlyFee = 1200m
                }
            ]);
        }

        if (snapshot.Customers.Count == 0)
        {
            var tariffs = snapshot.Tariffs.ToDictionary(tariff => tariff.Name);

            snapshot.Customers.AddRange(
            [
                new Customer
                {
                    AccountNumber = "100001",
                    FullName = "Иванов Иван Иванович",
                    Phone = "+7 900 111-22-33",
                    Address = "г. Махачкала, ул. Центральная, д. 12",
                    TariffPlanId = tariffs["Базовый"].Id
                },
                new Customer
                {
                    AccountNumber = "100002",
                    FullName = "Петрова Анна Сергеевна",
                    Phone = "+7 900 222-33-44",
                    Address = "г. Махачкала, пр-т Петра I, д. 8",
                    TariffPlanId = tariffs["Стандарт"].Id
                },
                new Customer
                {
                    AccountNumber = "100003",
                    FullName = "Алиев Магомед Русланович",
                    Phone = "+7 900 333-44-55",
                    Address = "г. Каспийск, ул. Ленина, д. 20",
                    TariffPlanId = tariffs["Премиум"].Id
                }
            ]);
        }

        if (snapshot.Payments.Count == 0)
        {
            var period = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var customers = snapshot.Customers.Take(2).ToArray();

            foreach (var customer in customers)
            {
                var tariff = snapshot.Tariffs.Single(item => item.Id == customer.TariffPlanId);
                snapshot.Payments.Add(new Payment
                {
                    CustomerId = customer.Id,
                    TariffPlanId = tariff.Id,
                    Period = period,
                    Amount = tariff.MonthlyFee,
                    Method = PaymentMethod.Card,
                    Note = "Демонстрационная оплата"
                });
            }
        }
    }
}
