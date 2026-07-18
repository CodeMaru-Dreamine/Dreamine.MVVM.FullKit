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
/// \if KO
/// <para>Page Database View Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Provides the ViewModel for the database sample page.</para>
/// \endif
/// </summary>
public sealed class PageDatabaseViewModel : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>selected Provider Tab 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the selected provider tab value.</para>
    /// \endif
    /// </summary>
    private DatabaseSampleTabViewModel? _selectedProviderTab;

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="PageDatabaseViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="PageDatabaseViewModel"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Provider Tabs 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the provider tabs value.</para>
    /// \endif
    /// </summary>
    public ObservableCollection<DatabaseSampleTabViewModel> ProviderTabs { get; }

    /// <summary>
    /// \if KO
    /// <para>Selected Provider Tab 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected provider tab value.</para>
    /// \endif
    /// </summary>
    public DatabaseSampleTabViewModel? SelectedProviderTab
    {
        get => _selectedProviderTab;
        set => SetProperty(ref _selectedProviderTab, value);
    }
}

/// <summary>
/// \if KO
/// <para>Database Sample Tab View Model 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates database sample tab view model functionality and related state.</para>
/// \endif
/// </summary>
public sealed class DatabaseSampleTabViewModel : ViewModelBase
{
    /// <summary>
    /// \if KO
    /// <para>provider Factory 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the provider factory value.</para>
    /// \endif
    /// </summary>
    private readonly Func<string, IDatabaseProvider> _providerFactory;
    /// <summary>
    /// \if KO
    /// <para>provider 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the provider value.</para>
    /// \endif
    /// </summary>
    private IDatabaseProvider? _provider;
    /// <summary>
    /// \if KO
    /// <para>selected Customer 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the selected customer value.</para>
    /// \endif
    /// </summary>
    private SampleCustomer? _selectedCustomer;
    /// <summary>
    /// \if KO
    /// <para>connection String 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the connection string value.</para>
    /// \endif
    /// </summary>
    private string _connectionString;
    /// <summary>
    /// \if KO
    /// <para>name Input 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the name input value.</para>
    /// \endif
    /// </summary>
    private string _nameInput = "Dreamine";
    /// <summary>
    /// \if KO
    /// <para>role Input 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the role input value.</para>
    /// \endif
    /// </summary>
    private string _roleInput = "Operator";
    /// <summary>
    /// \if KO
    /// <para>status Message 값을 보관합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Stores the status message value.</para>
    /// \endif
    /// </summary>
    private string _statusMessage = "Not initialized.";

    /// <summary>
    /// \if KO
    /// <para>지정한 설정으로 <see cref="DatabaseSampleTabViewModel"/> 클래스의 새 인스턴스를 초기화합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Initializes a new instance of the <see cref="DatabaseSampleTabViewModel"/> class with the specified settings.</para>
    /// \endif
    /// </summary>
    /// <param name="title">
    /// \if KO
    /// <para>title에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for title.</para>
    /// \endif
    /// </param>
    /// <param name="connectionString">
    /// \if KO
    /// <para>connection String에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for connection string.</para>
    /// \endif
    /// </param>
    /// <param name="providerFactory">
    /// \if KO
    /// <para>provider Factory에 사용할 <c>Func&lt;string, IDatabaseProvider&gt;</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>Func&lt;string, IDatabaseProvider&gt;</c> value used for provider factory.</para>
    /// \endif
    /// </param>
    /// <param name="autoInitialize">
    /// \if KO
    /// <para>auto Initialize에 사용할 <c>bool</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>bool</c> value used for auto initialize.</para>
    /// \endif
    /// </param>
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

    /// <summary>
    /// \if KO
    /// <para>Title 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the title value.</para>
    /// \endif
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// \if KO
    /// <para>Customers 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the customers value.</para>
    /// \endif
    /// </summary>
    public ObservableCollection<SampleCustomer> Customers { get; } = [];

    /// <summary>
    /// \if KO
    /// <para>Connection String 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the connection string value.</para>
    /// \endif
    /// </summary>
    public string ConnectionString
    {
        get => _connectionString;
        set => SetProperty(ref _connectionString, value);
    }

    /// <summary>
    /// \if KO
    /// <para>Selected Customer 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the selected customer value.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Name Input 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the name input value.</para>
    /// \endif
    /// </summary>
    public string NameInput
    {
        get => _nameInput;
        set => SetProperty(ref _nameInput, value);
    }

    /// <summary>
    /// \if KO
    /// <para>Role Input 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the role input value.</para>
    /// \endif
    /// </summary>
    public string RoleInput
    {
        get => _roleInput;
        set => SetProperty(ref _roleInput, value);
    }

    /// <summary>
    /// \if KO
    /// <para>Status Message 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the status message value.</para>
    /// \endif
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// \if KO
    /// <para>Initialize Command 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the initialize command value.</para>
    /// \endif
    /// </summary>
    public RelayCommand InitializeCommand => new(Initialize);

    /// <summary>
    /// \if KO
    /// <para>Add Command 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the add command value.</para>
    /// \endif
    /// </summary>
    public RelayCommand AddCommand => new(Add);

    /// <summary>
    /// \if KO
    /// <para>Update Command 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the update command value.</para>
    /// \endif
    /// </summary>
    public RelayCommand UpdateCommand => new(Update);

    /// <summary>
    /// \if KO
    /// <para>Delete Command 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the delete command value.</para>
    /// \endif
    /// </summary>
    public RelayCommand DeleteCommand => new(Delete);

    /// <summary>
    /// \if KO
    /// <para>Refresh Command 값을 가져옵니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets the refresh command value.</para>
    /// \endif
    /// </summary>
    public RelayCommand RefreshCommand => new(Refresh);

    /// <summary>
    /// \if KO
    /// <para>Initialize 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the initialize operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>항목을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the item.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Update 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the update operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Delete 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the delete operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Refresh 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the refresh operation.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Seed Data If Needed 항목을 추가합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Adds the seed data if needed item.</para>
    /// \endif
    /// </summary>
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

    /// <summary>
    /// \if KO
    /// <para>Get Provider 작업을 시도하고 성공 여부를 반환합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Attempts to get provider and returns whether the operation succeeds.</para>
    /// \endif
    /// </summary>
    /// <param name="provider">
    /// \if KO
    /// <para>provider에 사용할 <c>IDatabaseProvider</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>IDatabaseProvider</c> value used for provider.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Try Get Provider 조건이 충족되면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.</para>
    /// \endif
    /// \if EN
    /// <para><see langword="true"/> when the try get provider condition is satisfied; otherwise, <see langword="false"/>.</para>
    /// \endif
    /// </returns>
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

    /// <summary>
    /// \if KO
    /// <para>Normalize Input 작업을 수행합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Performs the normalize input operation.</para>
    /// \endif
    /// </summary>
    /// <param name="value">
    /// \if KO
    /// <para>적용할 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The value to apply.</para>
    /// \endif
    /// </param>
    /// <param name="fallback">
    /// \if KO
    /// <para>fallback에 사용할 <c>string</c> 값입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> value used for fallback.</para>
    /// \endif
    /// </param>
    /// <returns>
    /// \if KO
    /// <para>Normalize Input 작업에서 생성한 <c>string</c> 결과입니다.</para>
    /// \endif
    /// \if EN
    /// <para>The <c>string</c> result produced by the normalize input operation.</para>
    /// \endif
    /// </returns>
    private static string NormalizeInput(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();
    }
}

/// <summary>
/// \if KO
/// <para>Sample Customer 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates sample customer functionality and related state.</para>
/// \endif
/// </summary>
[DatabaseTable("SampleCustomers")]
public sealed class SampleCustomer
{
    /// <summary>
    /// \if KO
    /// <para>Id 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the id value.</para>
    /// \endif
    /// </summary>
    [DatabaseKey]
    [DatabaseGenerated]
    public int Id { get; set; }

    /// <summary>
    /// \if KO
    /// <para>Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the name value.</para>
    /// \endif
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>Role 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the role value.</para>
    /// \endif
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>Created At 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the created at value.</para>
    /// \endif
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
