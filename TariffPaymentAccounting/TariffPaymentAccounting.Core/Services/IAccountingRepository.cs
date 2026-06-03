using TariffPaymentAccounting.Core.Models;

namespace TariffPaymentAccounting.Core.Services;

public interface IAccountingRepository
{
    AccountingSnapshot Load();

    void Save(AccountingSnapshot snapshot);
}
