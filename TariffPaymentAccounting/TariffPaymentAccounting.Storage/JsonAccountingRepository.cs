using System.Text.Json;
using TariffPaymentAccounting.Core.Models;
using TariffPaymentAccounting.Core.Services;

namespace TariffPaymentAccounting.Storage;

public sealed class JsonAccountingRepository : IAccountingRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonAccountingRepository(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Путь к файлу хранения не задан.", nameof(filePath));
        }

        FilePath = filePath;
    }

    public string FilePath { get; }

    public bool Exists => File.Exists(FilePath);

    public static JsonAccountingRepository CreateDefault()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var directory = Path.Combine(appData, "TariffPaymentAccounting");
        return new JsonAccountingRepository(Path.Combine(directory, "tariff-payments.json"));
    }

    public AccountingSnapshot Load()
    {
        if (!File.Exists(FilePath))
        {
            return new AccountingSnapshot();
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<AccountingSnapshot>(json, SerializerOptions) ?? new AccountingSnapshot();
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Файл хранения поврежден или имеет неверный формат.", exception);
        }
    }

    public void Save(AccountingSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = $"{FilePath}.tmp";
        var json = JsonSerializer.Serialize(snapshot, SerializerOptions);
        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, FilePath, true);
    }
}
