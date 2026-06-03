using System.Text;
using TariffPaymentAccounting.Core.Models;
using TariffPaymentAccounting.Core.Services;
using TariffPaymentAccounting.Storage;

namespace TariffPaymentAccounting.App;

public sealed class MainForm : Form
{
    private readonly JsonAccountingRepository _repository;
    private readonly TariffPaymentService _service;
    private bool _isRefreshing;

    private DataGridView _customersGrid = null!;
    private DataGridView _tariffsGrid = null!;
    private DataGridView _paymentsGrid = null!;
    private DataGridView _reportGrid = null!;

    private TextBox _customerAccountBox = null!;
    private TextBox _customerNameBox = null!;
    private TextBox _customerPhoneBox = null!;
    private TextBox _customerAddressBox = null!;
    private ComboBox _customerTariffCombo = null!;
    private CheckBox _customerActiveCheck = null!;

    private TextBox _tariffNameBox = null!;
    private NumericUpDown _tariffFeeBox = null!;
    private TextBox _tariffDescriptionBox = null!;
    private CheckBox _tariffArchivedCheck = null!;

    private ComboBox _paymentCustomerCombo = null!;
    private ComboBox _paymentTariffCombo = null!;
    private DateTimePicker _paymentPeriodPicker = null!;
    private NumericUpDown _paymentAmountBox = null!;
    private ComboBox _paymentMethodCombo = null!;
    private TextBox _paymentNoteBox = null!;

    private DateTimePicker _reportPeriodPicker = null!;
    private Label _reportTotalsLabel = null!;
    private ToolStripStatusLabel _statusLabel = null!;
    private ToolStripStatusLabel _pathLabel = null!;

    public MainForm()
    {
        _repository = JsonAccountingRepository.CreateDefault();
        var isFirstRun = !_repository.Exists;
        var snapshot = _repository.Load();
        if (isFirstRun)
        {
            AccountingSeeder.SeedIfEmpty(snapshot);
        }

        _service = new TariffPaymentService(snapshot);
        _repository.Save(_service.Snapshot);

        BuildInterface();
        RefreshAll();
        SetStatus("Система учета оплаты тарифов готова к работе.");
    }

    private void BuildInterface()
    {
        Text = "Информационная система учета оплаты тарифов";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 720);
        Size = new Size(1280, 820);
        AutoScaleMode = AutoScaleMode.Font;

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill
        };

        tabs.TabPages.Add(BuildCustomersTab());
        tabs.TabPages.Add(BuildTariffsTab());
        tabs.TabPages.Add(BuildPaymentsTab());
        tabs.TabPages.Add(BuildReportTab());

        _statusLabel = new ToolStripStatusLabel();
        _pathLabel = new ToolStripStatusLabel
        {
            Spring = true,
            TextAlign = ContentAlignment.MiddleRight,
            Text = $"Файл данных: {_repository.FilePath}"
        };

        var statusStrip = new StatusStrip();
        statusStrip.Items.Add(_statusLabel);
        statusStrip.Items.Add(_pathLabel);

        Controls.Add(tabs);
        Controls.Add(statusStrip);
    }

    private TabPage BuildCustomersTab()
    {
        var page = new TabPage("Клиенты");
        var split = CreateSplitContainer();

        _customersGrid = CreateGrid();
        _customersGrid.SelectionChanged += (_, _) => LoadSelectedCustomer();

        var editor = CreateEditorLayout();
        _customerAccountBox = CreateTextBox();
        _customerNameBox = CreateTextBox();
        _customerPhoneBox = CreateTextBox();
        _customerAddressBox = CreateTextBox(multiline: true);
        _customerTariffCombo = CreateComboBox();
        _customerActiveCheck = new CheckBox
        {
            Text = "Клиент активен",
            Checked = true,
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 8)
        };

        AddField(editor, "Лицевой счет", _customerAccountBox);
        AddField(editor, "ФИО клиента", _customerNameBox);
        AddField(editor, "Телефон", _customerPhoneBox);
        AddField(editor, "Адрес", _customerAddressBox);
        AddField(editor, "Тариф", _customerTariffCombo);
        editor.Controls.Add(_customerActiveCheck, 0, editor.RowCount++);

        var addButton = CreateButton("Добавить");
        addButton.Click += (_, _) => TryRun(() =>
        {
            _service.AddCustomer(
                _customerAccountBox.Text,
                _customerNameBox.Text,
                _customerPhoneBox.Text,
                _customerAddressBox.Text,
                RequireSelectedGuid(_customerTariffCombo, "Тариф"));
            SaveAndRefresh("Клиент добавлен.");
            ClearCustomerEditor();
        });

        var saveButton = CreateButton("Сохранить");
        saveButton.Click += (_, _) => TryRun(() =>
        {
            var row = RequireSelectedRow<CustomerRow>(_customersGrid, "Выберите клиента.");
            _service.UpdateCustomer(
                row.Id,
                _customerAccountBox.Text,
                _customerNameBox.Text,
                _customerPhoneBox.Text,
                _customerAddressBox.Text,
                RequireSelectedGuid(_customerTariffCombo, "Тариф"),
                _customerActiveCheck.Checked);
            SaveAndRefresh("Данные клиента сохранены.");
        });

        var deactivateButton = CreateButton("Отключить");
        deactivateButton.Click += (_, _) => TryRun(() =>
        {
            var row = RequireSelectedRow<CustomerRow>(_customersGrid, "Выберите клиента.");
            if (!Confirm($"Отключить клиента \"{row.FullName}\"?"))
            {
                return;
            }

            _service.DeactivateCustomer(row.Id);
            SaveAndRefresh("Клиент отключен.");
        });

        var clearButton = CreateButton("Очистить");
        clearButton.Click += (_, _) => ClearCustomerEditor();

        AddButtons(editor, addButton, saveButton, deactivateButton, clearButton);

        split.Panel1.Controls.Add(_customersGrid);
        split.Panel2.Controls.Add(editor);
        page.Controls.Add(split);
        return page;
    }

    private TabPage BuildTariffsTab()
    {
        var page = new TabPage("Тарифы");
        var split = CreateSplitContainer();

        _tariffsGrid = CreateGrid();
        _tariffsGrid.SelectionChanged += (_, _) => LoadSelectedTariff();

        var editor = CreateEditorLayout();
        _tariffNameBox = CreateTextBox();
        _tariffFeeBox = CreateMoneyBox();
        _tariffDescriptionBox = CreateTextBox(multiline: true);
        _tariffArchivedCheck = new CheckBox
        {
            Text = "Тариф находится в архиве",
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 8)
        };

        AddField(editor, "Название", _tariffNameBox);
        AddField(editor, "Абонентская плата", _tariffFeeBox);
        AddField(editor, "Описание", _tariffDescriptionBox);
        editor.Controls.Add(_tariffArchivedCheck, 0, editor.RowCount++);

        var addButton = CreateButton("Добавить");
        addButton.Click += (_, _) => TryRun(() =>
        {
            _service.AddTariff(_tariffNameBox.Text, _tariffFeeBox.Value, _tariffDescriptionBox.Text);
            SaveAndRefresh("Тариф добавлен.");
            ClearTariffEditor();
        });

        var saveButton = CreateButton("Сохранить");
        saveButton.Click += (_, _) => TryRun(() =>
        {
            var row = RequireSelectedRow<TariffRow>(_tariffsGrid, "Выберите тариф.");
            _service.UpdateTariff(
                row.Id,
                _tariffNameBox.Text,
                _tariffFeeBox.Value,
                _tariffDescriptionBox.Text,
                _tariffArchivedCheck.Checked);
            SaveAndRefresh("Тариф сохранен.");
        });

        var archiveButton = CreateButton("В архив");
        archiveButton.Click += (_, _) => TryRun(() =>
        {
            var row = RequireSelectedRow<TariffRow>(_tariffsGrid, "Выберите тариф.");
            if (!Confirm($"Перенести тариф \"{row.Name}\" в архив?"))
            {
                return;
            }

            _service.ArchiveTariff(row.Id);
            SaveAndRefresh("Тариф перенесен в архив.");
        });

        var clearButton = CreateButton("Очистить");
        clearButton.Click += (_, _) => ClearTariffEditor();

        AddButtons(editor, addButton, saveButton, archiveButton, clearButton);

        split.Panel1.Controls.Add(_tariffsGrid);
        split.Panel2.Controls.Add(editor);
        page.Controls.Add(split);
        return page;
    }

    private TabPage BuildPaymentsTab()
    {
        var page = new TabPage("Платежи");
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var editor = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 6,
            Padding = new Padding(12)
        };

        for (var i = 0; i < editor.ColumnCount; i++)
        {
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / editor.ColumnCount));
        }

        _paymentCustomerCombo = CreateComboBox();
        _paymentCustomerCombo.SelectedIndexChanged += (_, _) => UpdatePaymentTariffFromCustomer();
        _paymentTariffCombo = CreateComboBox();
        _paymentTariffCombo.Enabled = false;
        _paymentPeriodPicker = CreateMonthPicker();
        _paymentAmountBox = CreateMoneyBox();
        _paymentMethodCombo = CreateComboBox();
        _paymentNoteBox = CreateTextBox();

        AddInlineField(editor, 0, "Клиент", _paymentCustomerCombo);
        AddInlineField(editor, 1, "Тариф", _paymentTariffCombo);
        AddInlineField(editor, 2, "Период", _paymentPeriodPicker);
        AddInlineField(editor, 3, "Сумма", _paymentAmountBox);
        AddInlineField(editor, 4, "Способ", _paymentMethodCombo);
        AddInlineField(editor, 5, "Примечание", _paymentNoteBox);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(12, 0, 12, 8),
            FlowDirection = FlowDirection.LeftToRight
        };

        var addButton = CreateButton("Принять оплату");
        addButton.Click += (_, _) => TryRun(() =>
        {
            _service.AddPayment(
                RequireSelectedGuid(_paymentCustomerCombo, "Клиент"),
                _paymentPeriodPicker.Value,
                _paymentAmountBox.Value,
                RequireSelectedPaymentMethod(),
                _paymentNoteBox.Text);
            SaveAndRefresh("Платеж принят.");
            _paymentNoteBox.Clear();
        });

        var deleteButton = CreateButton("Удалить платеж");
        deleteButton.Click += (_, _) => TryRun(() =>
        {
            var row = RequireSelectedRow<PaymentRow>(_paymentsGrid, "Выберите платеж.");
            if (!Confirm($"Удалить платеж клиента \"{row.Customer}\" на сумму {row.Amount:n2}?"))
            {
                return;
            }

            _service.RemovePayment(row.Id);
            SaveAndRefresh("Платеж удален.");
        });

        buttons.Controls.Add(addButton);
        buttons.Controls.Add(deleteButton);

        var top = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true
        };
        top.Controls.Add(buttons);
        top.Controls.Add(editor);

        _paymentsGrid = CreateGrid();

        layout.Controls.Add(top, 0, 0);
        layout.Controls.Add(_paymentsGrid, 0, 1);
        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildReportTab()
    {
        var page = new TabPage("Отчет");
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(12),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        _reportPeriodPicker = CreateMonthPicker();
        _reportPeriodPicker.ValueChanged += (_, _) => RefreshReport();

        var refreshButton = CreateButton("Обновить");
        refreshButton.Click += (_, _) => RefreshReport();

        var exportButton = CreateButton("Экспорт CSV");
        exportButton.Click += (_, _) => TryRun(ExportReport);

        _reportTotalsLabel = new Label
        {
            AutoSize = true,
            Margin = new Padding(18, 8, 0, 0),
            Font = new Font(Font, FontStyle.Bold)
        };

        panel.Controls.Add(new Label
        {
            Text = "Период:",
            AutoSize = true,
            Margin = new Padding(0, 8, 6, 0)
        });
        panel.Controls.Add(_reportPeriodPicker);
        panel.Controls.Add(refreshButton);
        panel.Controls.Add(exportButton);
        panel.Controls.Add(_reportTotalsLabel);

        _reportGrid = CreateGrid();

        layout.Controls.Add(panel, 0, 0);
        layout.Controls.Add(_reportGrid, 0, 1);
        page.Controls.Add(layout);
        return page;
    }

    private void RefreshAll()
    {
        _isRefreshing = true;
        try
        {
            RefreshLookups();
            RefreshCustomersGrid();
            RefreshTariffsGrid();
            RefreshPaymentsGrid();
            RefreshReport();
            UpdatePaymentTariffFromCustomer();
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private void RefreshLookups()
    {
        SetComboItems(
            _customerTariffCombo,
            _service.Tariffs.Select(tariff => new LookupItem<Guid>(
                tariff.Id,
                tariff.IsArchived ? $"{tariff.Name} (архив)" : $"{tariff.Name} - {tariff.MonthlyFee:n2}")));

        SetComboItems(
            _paymentCustomerCombo,
            _service.Customers
                .Where(customer => customer.IsActive)
                .Select(customer => new LookupItem<Guid>(
                    customer.Id,
                    $"{customer.AccountNumber} - {customer.FullName}")));

        SetComboItems(
            _paymentTariffCombo,
            _service.Tariffs.Select(tariff => new LookupItem<Guid>(
                tariff.Id,
                $"{tariff.Name} - {tariff.MonthlyFee:n2}")));

        SetComboItems(
            _paymentMethodCombo,
            Enum.GetValues<PaymentMethod>()
                .Select(method => new LookupItem<PaymentMethod>(method, FormatMethod(method))));
    }

    private void RefreshCustomersGrid()
    {
        _customersGrid.DataSource = _service.Customers
            .Select(customer => new CustomerRow
            {
                Id = customer.Id,
                AccountNumber = customer.AccountNumber,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Address = customer.Address,
                Tariff = _service.GetTariffName(customer.TariffPlanId),
                State = customer.IsActive ? "Активен" : "Отключен"
            })
            .ToList();

        HideColumn(_customersGrid, nameof(CustomerRow.Id));
        SetHeader(_customersGrid, nameof(CustomerRow.AccountNumber), "Лицевой счет");
        SetHeader(_customersGrid, nameof(CustomerRow.FullName), "ФИО");
        SetHeader(_customersGrid, nameof(CustomerRow.Phone), "Телефон");
        SetHeader(_customersGrid, nameof(CustomerRow.Address), "Адрес");
        SetHeader(_customersGrid, nameof(CustomerRow.Tariff), "Тариф");
        SetHeader(_customersGrid, nameof(CustomerRow.State), "Статус");
    }

    private void RefreshTariffsGrid()
    {
        _tariffsGrid.DataSource = _service.Tariffs
            .Select(tariff => new TariffRow
            {
                Id = tariff.Id,
                Name = tariff.Name,
                MonthlyFee = tariff.MonthlyFee,
                Description = tariff.Description,
                State = tariff.IsArchived ? "Архив" : "Активен"
            })
            .ToList();

        HideColumn(_tariffsGrid, nameof(TariffRow.Id));
        SetHeader(_tariffsGrid, nameof(TariffRow.Name), "Название");
        SetHeader(_tariffsGrid, nameof(TariffRow.MonthlyFee), "Абонентская плата");
        SetHeader(_tariffsGrid, nameof(TariffRow.Description), "Описание");
        SetHeader(_tariffsGrid, nameof(TariffRow.State), "Статус");
        SetMoneyFormat(_tariffsGrid, nameof(TariffRow.MonthlyFee));
    }

    private void RefreshPaymentsGrid()
    {
        _paymentsGrid.DataSource = _service.Payments
            .Select(payment => new PaymentRow
            {
                Id = payment.Id,
                PaidAt = payment.PaidAt.ToString("dd.MM.yyyy HH:mm"),
                Period = payment.Period.ToString("MM.yyyy"),
                Customer = _service.GetCustomerName(payment.CustomerId),
                Tariff = _service.GetTariffName(payment.TariffPlanId),
                Amount = payment.Amount,
                Method = FormatMethod(payment.Method),
                Note = payment.Note
            })
            .ToList();

        HideColumn(_paymentsGrid, nameof(PaymentRow.Id));
        SetHeader(_paymentsGrid, nameof(PaymentRow.PaidAt), "Дата оплаты");
        SetHeader(_paymentsGrid, nameof(PaymentRow.Period), "Период");
        SetHeader(_paymentsGrid, nameof(PaymentRow.Customer), "Клиент");
        SetHeader(_paymentsGrid, nameof(PaymentRow.Tariff), "Тариф");
        SetHeader(_paymentsGrid, nameof(PaymentRow.Amount), "Сумма");
        SetHeader(_paymentsGrid, nameof(PaymentRow.Method), "Способ");
        SetHeader(_paymentsGrid, nameof(PaymentRow.Note), "Примечание");
        SetMoneyFormat(_paymentsGrid, nameof(PaymentRow.Amount));
    }

    private void RefreshReport()
    {
        if (_reportGrid is null || _reportTotalsLabel is null)
        {
            return;
        }

        var rows = _service.BuildMonthlyReport(_reportPeriodPicker.Value)
            .Select(charge => new ReportRow
            {
                AccountNumber = charge.AccountNumber,
                Customer = charge.CustomerName,
                Tariff = charge.TariffName,
                RequiredAmount = charge.RequiredAmount,
                PaidAmount = charge.PaidAmount,
                Debt = Math.Max(0, -charge.Balance),
                Overpayment = Math.Max(0, charge.Balance),
                Status = FormatStatus(charge.Status)
            })
            .ToList();

        _reportGrid.DataSource = rows;
        SetHeader(_reportGrid, nameof(ReportRow.AccountNumber), "Лицевой счет");
        SetHeader(_reportGrid, nameof(ReportRow.Customer), "Клиент");
        SetHeader(_reportGrid, nameof(ReportRow.Tariff), "Тариф");
        SetHeader(_reportGrid, nameof(ReportRow.RequiredAmount), "Начислено");
        SetHeader(_reportGrid, nameof(ReportRow.PaidAmount), "Оплачено");
        SetHeader(_reportGrid, nameof(ReportRow.Debt), "Долг");
        SetHeader(_reportGrid, nameof(ReportRow.Overpayment), "Переплата");
        SetHeader(_reportGrid, nameof(ReportRow.Status), "Статус");
        SetMoneyFormat(_reportGrid, nameof(ReportRow.RequiredAmount));
        SetMoneyFormat(_reportGrid, nameof(ReportRow.PaidAmount));
        SetMoneyFormat(_reportGrid, nameof(ReportRow.Debt));
        SetMoneyFormat(_reportGrid, nameof(ReportRow.Overpayment));

        var required = rows.Sum(row => row.RequiredAmount);
        var paid = rows.Sum(row => row.PaidAmount);
        var debt = rows.Sum(row => row.Debt);
        _reportTotalsLabel.Text = $"Начислено: {required:n2}   Оплачено: {paid:n2}   Долг: {debt:n2}";
    }

    private void LoadSelectedCustomer()
    {
        if (_isRefreshing)
        {
            return;
        }

        if (SelectedRow<CustomerRow>(_customersGrid) is not { } row)
        {
            return;
        }

        var customer = _service.GetCustomer(row.Id);
        _customerAccountBox.Text = customer.AccountNumber;
        _customerNameBox.Text = customer.FullName;
        _customerPhoneBox.Text = customer.Phone;
        _customerAddressBox.Text = customer.Address;
        _customerActiveCheck.Checked = customer.IsActive;
        SelectComboValue(_customerTariffCombo, customer.TariffPlanId);
    }

    private void LoadSelectedTariff()
    {
        if (_isRefreshing)
        {
            return;
        }

        if (SelectedRow<TariffRow>(_tariffsGrid) is not { } row)
        {
            return;
        }

        var tariff = _service.GetTariff(row.Id);
        _tariffNameBox.Text = tariff.Name;
        _tariffFeeBox.Value = ClampMoney(tariff.MonthlyFee);
        _tariffDescriptionBox.Text = tariff.Description;
        _tariffArchivedCheck.Checked = tariff.IsArchived;
    }

    private void UpdatePaymentTariffFromCustomer()
    {
        if (_paymentCustomerCombo is null || _paymentTariffCombo is null || _paymentAmountBox is null)
        {
            return;
        }

        if (SelectedLookup<Guid>(_paymentCustomerCombo) is not { } customerId)
        {
            return;
        }

        var customer = _service.GetCustomer(customerId);
        var tariff = _service.GetTariff(customer.TariffPlanId);
        SelectComboValue(_paymentTariffCombo, tariff.Id);
        _paymentAmountBox.Value = ClampMoney(tariff.MonthlyFee);
    }

    private void ClearCustomerEditor()
    {
        _customerAccountBox.Clear();
        _customerNameBox.Clear();
        _customerPhoneBox.Clear();
        _customerAddressBox.Clear();
        _customerActiveCheck.Checked = true;
        if (_customerTariffCombo.Items.Count > 0)
        {
            _customerTariffCombo.SelectedIndex = 0;
        }
    }

    private void ClearTariffEditor()
    {
        _tariffNameBox.Clear();
        _tariffDescriptionBox.Clear();
        _tariffFeeBox.Value = 1;
        _tariffArchivedCheck.Checked = false;
    }

    private void SaveAndRefresh(string message)
    {
        _repository.Save(_service.Snapshot);
        RefreshAll();
        SetStatus(message);
    }

    private void ExportReport()
    {
        var rows = _service.BuildMonthlyReport(_reportPeriodPicker.Value);
        if (rows.Count == 0)
        {
            MessageBox.Show(this, "Нет данных для экспорта.", "Экспорт отчета", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Сохранить отчет",
            Filter = "CSV-файл (*.csv)|*.csv|Все файлы (*.*)|*.*",
            FileName = $"Отчет_оплаты_{_reportPeriodPicker.Value:yyyy_MM}.csv",
            AddExtension = true,
            DefaultExt = "csv"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("Лицевой счет;Клиент;Тариф;Период;Начислено;Оплачено;Долг;Переплата;Статус");

        foreach (var row in rows)
        {
            var debt = Math.Max(0, -row.Balance);
            var overpayment = Math.Max(0, row.Balance);
            builder.AppendLine(string.Join(';',
                EscapeCsv(row.AccountNumber),
                EscapeCsv(row.CustomerName),
                EscapeCsv(row.TariffName),
                row.Period.ToString("MM.yyyy"),
                row.RequiredAmount.ToString("0.00"),
                row.PaidAmount.ToString("0.00"),
                debt.ToString("0.00"),
                overpayment.ToString("0.00"),
                EscapeCsv(FormatStatus(row.Status))));
        }

        File.WriteAllText(dialog.FileName, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        SetStatus($"Отчет сохранен: {dialog.FileName}");
    }

    private void TryRun(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus(exception.Message);
        }
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = message;
    }

    private bool Confirm(string message)
    {
        return MessageBox.Show(this, message, "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    }

    private PaymentMethod RequireSelectedPaymentMethod()
    {
        if (SelectedLookup<PaymentMethod>(_paymentMethodCombo) is { } method)
        {
            return method;
        }

        throw new InvalidOperationException("Выберите способ оплаты.");
    }

    private static SplitContainer CreateSplitContainer() => new()
    {
        Dock = DockStyle.Fill,
        FixedPanel = FixedPanel.Panel2,
        SplitterDistance = 780
    };

    private static DataGridView CreateGrid() => new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        BackgroundColor = SystemColors.Window,
        BorderStyle = BorderStyle.None,
        CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
        EnableHeadersVisualStyles = false,
        MultiSelect = false,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(238, 242, 247),
            ForeColor = Color.FromArgb(30, 41, 59),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        },
        AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(248, 250, 252)
        }
    };

    private static TableLayoutPanel CreateEditorLayout() => new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        ColumnCount = 1,
        Padding = new Padding(12)
    };

    private static TextBox CreateTextBox(bool multiline = false) => new()
    {
        Dock = DockStyle.Top,
        Multiline = multiline,
        Height = multiline ? 78 : 28,
        Margin = new Padding(0, 0, 0, 6)
    };

    private static ComboBox CreateComboBox() => new()
    {
        Dock = DockStyle.Top,
        DropDownStyle = ComboBoxStyle.DropDownList,
        Height = 28,
        Margin = new Padding(0, 0, 0, 6)
    };

    private static NumericUpDown CreateMoneyBox() => new()
    {
        DecimalPlaces = 2,
        Maximum = 1_000_000,
        Minimum = 0,
        ThousandsSeparator = true,
        Dock = DockStyle.Top,
        Height = 28,
        Margin = new Padding(0, 0, 0, 6)
    };

    private static DateTimePicker CreateMonthPicker() => new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "MMMM yyyy",
        ShowUpDown = true,
        Width = 160,
        Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
    };

    private static Button CreateButton(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Height = 32,
        Margin = new Padding(0, 8, 8, 0),
        Padding = new Padding(10, 4, 10, 4)
    };

    private static void AddField(TableLayoutPanel layout, string caption, Control control)
    {
        var label = new Label
        {
            Text = caption,
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 2)
        };

        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(label, 0, layout.RowCount++);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(control, 0, layout.RowCount++);
    }

    private static void AddInlineField(TableLayoutPanel layout, int column, string caption, Control control)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0, 0, 10, 0)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(new Label
        {
            Text = caption,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 2)
        }, 0, 0);
        panel.Controls.Add(control, 0, 1);
        layout.Controls.Add(panel, column, 0);
    }

    private static void AddButtons(TableLayoutPanel layout, params Button[] buttons)
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 8, 0, 0)
        };

        panel.Controls.AddRange(buttons);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(panel, 0, layout.RowCount++);
    }

    private static void SetComboItems<T>(ComboBox comboBox, IEnumerable<LookupItem<T>> items)
    {
        comboBox.BeginUpdate();
        comboBox.Items.Clear();
        foreach (var item in items)
        {
            comboBox.Items.Add(item);
        }

        if (comboBox.Items.Count > 0)
        {
            comboBox.SelectedIndex = 0;
        }

        comboBox.EndUpdate();
    }

    private static Guid RequireSelectedGuid(ComboBox comboBox, string fieldName)
    {
        if (SelectedLookup<Guid>(comboBox) is { } value)
        {
            return value;
        }

        throw new InvalidOperationException($"Выберите значение поля \"{fieldName}\".");
    }

    private static T? SelectedLookup<T>(ComboBox comboBox) where T : struct
    {
        return comboBox.SelectedItem is LookupItem<T> item ? item.Value : null;
    }

    private static void SelectComboValue<T>(ComboBox comboBox, T value)
    {
        foreach (var item in comboBox.Items)
        {
            if (item is LookupItem<T> lookup && EqualityComparer<T>.Default.Equals(lookup.Value, value))
            {
                comboBox.SelectedItem = item;
                return;
            }
        }
    }

    private static T? SelectedRow<T>(DataGridView grid) where T : class
    {
        return grid.CurrentRow?.DataBoundItem as T;
    }

    private static T RequireSelectedRow<T>(DataGridView grid, string message) where T : class
    {
        return SelectedRow<T>(grid) ?? throw new InvalidOperationException(message);
    }

    private static decimal ClampMoney(decimal value)
    {
        return Math.Min(1_000_000, Math.Max(0, value));
    }

    private static void HideColumn(DataGridView grid, string columnName)
    {
        if (grid.Columns.Contains(columnName))
        {
            grid.Columns[columnName]!.Visible = false;
        }
    }

    private static void SetHeader(DataGridView grid, string columnName, string header)
    {
        if (grid.Columns.Contains(columnName))
        {
            grid.Columns[columnName]!.HeaderText = header;
        }
    }

    private static void SetMoneyFormat(DataGridView grid, string columnName)
    {
        if (grid.Columns.Contains(columnName))
        {
            grid.Columns[columnName]!.DefaultCellStyle.Format = "n2";
            grid.Columns[columnName]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static string FormatMethod(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Наличные",
        PaymentMethod.Card => "Банковская карта",
        PaymentMethod.BankTransfer => "Банковский перевод",
        PaymentMethod.Online => "Онлайн",
        _ => method.ToString()
    };

    private static string FormatStatus(PaymentStatus status) => status switch
    {
        PaymentStatus.Unpaid => "Не оплачено",
        PaymentStatus.Partial => "Частично",
        PaymentStatus.Paid => "Оплачено",
        PaymentStatus.Overpaid => "Переплата",
        _ => status.ToString()
    };

    private sealed record LookupItem<T>(T Value, string Text)
    {
        public override string ToString() => Text;
    }

    private sealed class CustomerRow
    {
        public Guid Id { get; init; }
        public string AccountNumber { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string Phone { get; init; } = string.Empty;
        public string Address { get; init; } = string.Empty;
        public string Tariff { get; init; } = string.Empty;
        public string State { get; init; } = string.Empty;
    }

    private sealed class TariffRow
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public decimal MonthlyFee { get; init; }
        public string Description { get; init; } = string.Empty;
        public string State { get; init; } = string.Empty;
    }

    private sealed class PaymentRow
    {
        public Guid Id { get; init; }
        public string PaidAt { get; init; } = string.Empty;
        public string Period { get; init; } = string.Empty;
        public string Customer { get; init; } = string.Empty;
        public string Tariff { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public string Method { get; init; } = string.Empty;
        public string Note { get; init; } = string.Empty;
    }

    private sealed class ReportRow
    {
        public string AccountNumber { get; init; } = string.Empty;
        public string Customer { get; init; } = string.Empty;
        public string Tariff { get; init; } = string.Empty;
        public decimal RequiredAmount { get; init; }
        public decimal PaidAmount { get; init; }
        public decimal Debt { get; init; }
        public decimal Overpayment { get; init; }
        public string Status { get; init; } = string.Empty;
    }
}
