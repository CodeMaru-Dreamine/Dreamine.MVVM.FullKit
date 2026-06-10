using System.Collections.ObjectModel;
using System.IO;
using Dreamine.Database.Abstractions;
using Dreamine.Database.Abstractions.Mapping;
using Dreamine.Database.MySql;
using Dreamine.Database.Oracle;
using Dreamine.Database.Sqlite;
using Dreamine.Database.SqlServer;
using Dreamine.MVVM.ViewModels;

namespace SampleSmart.Pages.PageSub;

/// <summary>
/// Provides the ViewModel for the database sample page.
/// </summary>
public sealed class PageDatabaseViewModel : ViewModelBase
{
    private DatabaseSampleTabViewModel? _selectedProviderTab;

    public PageDatabaseViewModel()
    {
        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dataDirectory);

        var sqlitePath = Path.Combine(dataDirectory, "SampleSmart.db");

        ProviderTabs =
        [
            new DatabaseSampleTabViewModel(
                "SQLite",
                $"Data Source={sqlitePath}",
                connectionString => new SqliteDatabaseProvider(connectionString),
                autoInitialize: true),

            new DatabaseSampleTabViewModel(
                "MySQL",
                "Server=localhost;Port=3306;Database=dreamine_sample;User ID=root;Password=password;",
                connectionString => new MySqlDatabaseProvider(connectionString)),

            new DatabaseSampleTabViewModel(
                "Oracle",
                "User Id=dreamine;Password=password;Data Source=localhost:1521/XEPDB1;",
                connectionString => new OracleDatabaseProvider(connectionString)),

            new DatabaseSampleTabViewModel(
                "MS SQL",
                "Server=localhost;Database=dreamine_sample;User Id=sa;Password=password;TrustServerCertificate=True;",
                connectionString => new SqlServerDatabaseProvider(connectionString))
        ];

        SelectedProviderTab = ProviderTabs[0];
    }

    public ObservableCollection<DatabaseSampleTabViewModel> ProviderTabs { get; }

    public DatabaseSampleTabViewModel? SelectedProviderTab
    {
        get => _selectedProviderTab;
        set => SetProperty(ref _selectedProviderTab, value);
    }
}

public sealed class DatabaseSampleTabViewModel : ViewModelBase
{
    private readonly Func<string, IDatabaseProvider> _providerFactory;
    private IDatabaseProvider? _provider;
    private SampleCustomer? _selectedCustomer;
    private string _connectionString;
    private string _nameInput = "Dreamine";
    private string _roleInput = "Operator";
    private string _statusMessage = "Not initialized.";

    public DatabaseSampleTabViewModel(
        string title,
        string connectionString,
        Func<string, IDatabaseProvider> providerFactory,
        bool autoInitialize = false)
    {
        Title = title;
        _connectionString = connectionString;
        _providerFactory = providerFactory;

        if (autoInitialize)
        {
            Initialize();
        }
    }

    public string Title { get; }

    public ObservableCollection<SampleCustomer> Customers { get; } = [];

    public string ConnectionString
    {
        get => _connectionString;
        set => SetProperty(ref _connectionString, value);
    }

    public SampleCustomer? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (!SetProperty(ref _selectedCustomer, value))
            {
                return;
            }

            if (value is null)
            {
                return;
            }

            NameInput = value.Name;
            RoleInput = value.Role;
        }
    }

    public string NameInput
    {
        get => _nameInput;
        set => SetProperty(ref _nameInput, value);
    }

    public string RoleInput
    {
        get => _roleInput;
        set => SetProperty(ref _roleInput, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public RelayCommand InitializeCommand => new(Initialize);

    public RelayCommand AddCommand => new(Add);

    public RelayCommand UpdateCommand => new(Update);

    public RelayCommand DeleteCommand => new(Delete);

    public RelayCommand RefreshCommand => new(Refresh);

    private void Initialize()
    {
        try
        {
            _provider = _providerFactory(ConnectionString);
            _provider.EnsureDatabaseExists();
            _provider.CreateTable<SampleCustomer>();

            AddSeedDataIfNeeded();
            Refresh();
        }
        catch (Exception ex)
        {
            StatusMessage = $"{Title} initialize failed: {ex.Message}";
        }
    }

    private void Add()
    {
        if (!TryGetProvider(out var provider))
        {
            return;
        }

        try
        {
            var name = NormalizeInput(NameInput, "New customer");
            var role = NormalizeInput(RoleInput, "Viewer");

            provider.Insert(new SampleCustomer
            {
                Name = name,
                Role = role,
                CreatedAt = DateTime.Now
            });

            StatusMessage = $"{Title}: added {name}.";
            Refresh();
        }
        catch (Exception ex)
        {
            StatusMessage = $"{Title} add failed: {ex.Message}";
        }
    }

    private void Update()
    {
        if (!TryGetProvider(out var provider))
        {
            return;
        }

        if (SelectedCustomer is null)
        {
            StatusMessage = "Select a row to update.";
            return;
        }

        try
        {
            SelectedCustomer.Name = NormalizeInput(NameInput, SelectedCustomer.Name);
            SelectedCustomer.Role = NormalizeInput(RoleInput, SelectedCustomer.Role);

            provider.Update(SelectedCustomer);
            StatusMessage = $"{Title}: updated #{SelectedCustomer.Id}.";
            Refresh();
        }
        catch (Exception ex)
        {
            StatusMessage = $"{Title} update failed: {ex.Message}";
        }
    }

    private void Delete()
    {
        if (!TryGetProvider(out var provider))
        {
            return;
        }

        if (SelectedCustomer is null)
        {
            StatusMessage = "Select a row to delete.";
            return;
        }

        try
        {
            var deletedId = SelectedCustomer.Id;
            provider.Delete(SelectedCustomer);
            SelectedCustomer = null;
            StatusMessage = $"{Title}: deleted #{deletedId}.";
            Refresh();
        }
        catch (Exception ex)
        {
            StatusMessage = $"{Title} delete failed: {ex.Message}";
        }
    }

    private void Refresh()
    {
        if (!TryGetProvider(out var provider))
        {
            return;
        }

        try
        {
            var rows = provider
                .Query<SampleCustomer>("SELECT Id, Name, Role, CreatedAt FROM SampleCustomers ORDER BY Id DESC")
                .ToArray();

            Customers.Clear();
            foreach (var row in rows)
            {
                Customers.Add(row);
            }

            StatusMessage = $"{Title}: loaded {Customers.Count} rows.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"{Title} refresh failed: {ex.Message}";
        }
    }

    private void AddSeedDataIfNeeded()
    {
        if (!TryGetProvider(out var provider))
        {
            return;
        }

        var count = provider.ExecuteScalar<long>("SELECT COUNT(1) FROM SampleCustomers");
        if (count > 0)
        {
            return;
        }

        provider.Insert(new SampleCustomer
        {
            Name = "Minsu",
            Role = "Admin",
            CreatedAt = DateTime.Now
        });

        provider.Insert(new SampleCustomer
        {
            Name = "Sample Operator",
            Role = "Operator",
            CreatedAt = DateTime.Now
        });
    }

    private bool TryGetProvider(out IDatabaseProvider provider)
    {
        if (_provider is not null)
        {
            provider = _provider;
            return true;
        }

        StatusMessage = $"{Title}: initialize the provider first.";
        provider = null!;
        return false;
    }

    private static string NormalizeInput(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();
    }
}

[DatabaseTable("SampleCustomers")]
public sealed class SampleCustomer
{
    [DatabaseKey]
    [DatabaseGenerated]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
