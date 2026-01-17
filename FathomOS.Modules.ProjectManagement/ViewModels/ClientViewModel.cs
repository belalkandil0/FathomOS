using System.Collections.ObjectModel;
using System.Windows.Input;
using FathomOS.Modules.ProjectManagement.Models;
using FathomOS.Modules.ProjectManagement.Services;

namespace FathomOS.Modules.ProjectManagement.ViewModels;

/// <summary>
/// ViewModel for client list and editing
/// </summary>
public class ClientListViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;

    public ClientListViewModel(IProjectService projectService)
    {
        _projectService = projectService;

        // Initialize collections
        Clients = new ObservableCollection<Client>();
        FilteredClients = new ObservableCollection<Client>();
        ClientTypes = new ObservableCollection<ClientType?>();

        // Initialize client type filter options
        ClientTypes.Add(null); // All types
        foreach (ClientType type in Enum.GetValues(typeof(ClientType)))
            ClientTypes.Add(type);

        // Initialize commands
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        AddClientCommand = new RelayCommand(AddClient);
        EditClientCommand = new RelayCommand<Client>(EditClient, CanEditClient);
        DeleteClientCommand = new AsyncRelayCommand<Client>(DeleteClientAsync, CanDeleteClient);
        ClearFiltersCommand = new RelayCommand(ClearFilters);
    }

    #region Properties

    public ObservableCollection<Client> Clients { get; }
    public ObservableCollection<Client> FilteredClients { get; }
    public ObservableCollection<ClientType?> ClientTypes { get; }

    private Client? _selectedClient;
    public Client? SelectedClient
    {
        get => _selectedClient;
        set
        {
            if (SetProperty(ref _selectedClient, value))
            {
                OnPropertyChanged(nameof(HasSelectedClient));
                SelectedClientChanged?.Invoke(this, value);
            }
        }
    }

    public bool HasSelectedClient => SelectedClient != null;

    private string? _searchText;
    public string? SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilters();
            }
        }
    }

    private ClientType? _selectedClientType;
    public ClientType? SelectedClientType
    {
        get => _selectedClientType;
        set
        {
            if (SetProperty(ref _selectedClientType, value))
            {
                ApplyFilters();
            }
        }
    }

    private int _totalClientCount;
    public int TotalClientCount
    {
        get => _totalClientCount;
        set => SetProperty(ref _totalClientCount, value);
    }

    private int _activeClientCount;
    public int ActiveClientCount
    {
        get => _activeClientCount;
        set => SetProperty(ref _activeClientCount, value);
    }

    #endregion

    #region Commands

    public ICommand LoadCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand AddClientCommand { get; }
    public ICommand EditClientCommand { get; }
    public ICommand DeleteClientCommand { get; }
    public ICommand ClearFiltersCommand { get; }

    #endregion

    #region Events

    public event EventHandler<Client?>? SelectedClientChanged;
    public event EventHandler? AddClientRequested;
    public event EventHandler<Client>? EditClientRequested;
    public event EventHandler<Client>? ClientDeleted;

    #endregion

    #region Methods

    public async Task LoadAsync()
    {
        await ExecuteAsync(async () =>
        {
            var clients = await _projectService.GetAllClientsAsync();
            Clients.Clear();
            foreach (var client in clients)
            {
                Clients.Add(client);
            }

            TotalClientCount = clients.Count();
            ActiveClientCount = clients.Count(c => c.IsActive);

            ApplyFilters();
        }, "Loading clients...");
    }

    private void ApplyFilters()
    {
        var filtered = Clients.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLower();
            filtered = filtered.Where(c =>
                c.CompanyName.ToLower().Contains(search) ||
                (c.ShortName?.ToLower().Contains(search) ?? false) ||
                c.ClientCode.ToLower().Contains(search) ||
                (c.Country?.ToLower().Contains(search) ?? false));
        }

        // Apply client type filter
        if (SelectedClientType.HasValue)
        {
            filtered = filtered.Where(c => c.ClientType == SelectedClientType.Value);
        }

        FilteredClients.Clear();
        foreach (var client in filtered)
        {
            FilteredClients.Add(client);
        }
    }

    private void ClearFilters(object? parameter)
    {
        SearchText = null;
        SelectedClientType = null;
    }

    private void AddClient(object? parameter)
    {
        AddClientRequested?.Invoke(this, EventArgs.Empty);
    }

    private bool CanEditClient(Client? client)
    {
        return client != null;
    }

    private void EditClient(Client? client)
    {
        if (client != null)
        {
            EditClientRequested?.Invoke(this, client);
        }
    }

    private bool CanDeleteClient(Client? client)
    {
        return client != null;
    }

    private async Task DeleteClientAsync(Client? client)
    {
        if (client == null) return;

        await ExecuteAsync(async () =>
        {
            // Soft delete
            client.IsActive = false;
            await _projectService.UpdateClientAsync(client);
            Clients.Remove(client);
            FilteredClients.Remove(client);
            TotalClientCount--;
            ActiveClientCount--;
            ClientDeleted?.Invoke(this, client);
        }, "Deleting client...");
    }

    #endregion
}

/// <summary>
/// ViewModel for client detail/editing
/// </summary>
public class ClientDetailViewModel : ViewModelBase
{
    private readonly Client _original;
    private readonly bool _isNew;

    public ClientDetailViewModel(Client? client = null)
    {
        _isNew = client == null;
        _original = client ?? new Client();

        // Copy values for editing
        Client = new Client
        {
            ClientId = _original.ClientId,
            ClientCode = _original.ClientCode,
            CompanyName = _original.CompanyName,
            ShortName = _original.ShortName,
            ClientType = _original.ClientType,
            TaxId = _original.TaxId,
            Website = _original.Website,
            Email = _original.Email,
            Phone = _original.Phone,
            AddressLine1 = _original.AddressLine1,
            AddressLine2 = _original.AddressLine2,
            City = _original.City,
            StateProvince = _original.StateProvince,
            PostalCode = _original.PostalCode,
            Country = _original.Country,
            PaymentTerms = _original.PaymentTerms,
            CreditLimit = _original.CreditLimit,
            DefaultCurrency = _original.DefaultCurrency,
            Notes = _original.Notes,
            IsActive = _original.IsActive
        };

        // Initialize collections
        ClientTypes = new ObservableCollection<ClientType>();
        Currencies = new ObservableCollection<CurrencyCode>();

        foreach (ClientType type in Enum.GetValues(typeof(ClientType)))
            ClientTypes.Add(type);
        foreach (CurrencyCode currency in Enum.GetValues(typeof(CurrencyCode)))
            Currencies.Add(currency);

        // Initialize commands
        SaveCommand = new RelayCommand(_ => Save(null), _ => CanSave());
        CancelCommand = new RelayCommand(_ => Cancel(null));
    }

    #region Properties

    public Client Client { get; }

    public string Title => _isNew ? "Add Client" : "Edit Client";

    public ObservableCollection<ClientType> ClientTypes { get; }
    public ObservableCollection<CurrencyCode> Currencies { get; }

    public string CompanyName
    {
        get => Client.CompanyName;
        set
        {
            if (Client.CompanyName != value)
            {
                Client.CompanyName = value;
                OnPropertyChanged();
            }
        }
    }

    public string? ShortName
    {
        get => Client.ShortName;
        set
        {
            if (Client.ShortName != value)
            {
                Client.ShortName = value;
                OnPropertyChanged();
            }
        }
    }

    public string ClientCode
    {
        get => Client.ClientCode;
        set
        {
            if (Client.ClientCode != value)
            {
                Client.ClientCode = value;
                OnPropertyChanged();
            }
        }
    }

    public ClientType SelectedClientType
    {
        get => Client.ClientType;
        set
        {
            if (Client.ClientType != value)
            {
                Client.ClientType = value;
                OnPropertyChanged();
            }
        }
    }

    public string? Email
    {
        get => Client.Email;
        set
        {
            if (Client.Email != value)
            {
                Client.Email = value;
                OnPropertyChanged();
            }
        }
    }

    public string? Phone
    {
        get => Client.Phone;
        set
        {
            if (Client.Phone != value)
            {
                Client.Phone = value;
                OnPropertyChanged();
            }
        }
    }

    public string? Website
    {
        get => Client.Website;
        set
        {
            if (Client.Website != value)
            {
                Client.Website = value;
                OnPropertyChanged();
            }
        }
    }

    public string? AddressLine1
    {
        get => Client.AddressLine1;
        set
        {
            if (Client.AddressLine1 != value)
            {
                Client.AddressLine1 = value;
                OnPropertyChanged();
            }
        }
    }

    public string? AddressLine2
    {
        get => Client.AddressLine2;
        set
        {
            if (Client.AddressLine2 != value)
            {
                Client.AddressLine2 = value;
                OnPropertyChanged();
            }
        }
    }

    public string? City
    {
        get => Client.City;
        set
        {
            if (Client.City != value)
            {
                Client.City = value;
                OnPropertyChanged();
            }
        }
    }

    public string? StateProvince
    {
        get => Client.StateProvince;
        set
        {
            if (Client.StateProvince != value)
            {
                Client.StateProvince = value;
                OnPropertyChanged();
            }
        }
    }

    public string? PostalCode
    {
        get => Client.PostalCode;
        set
        {
            if (Client.PostalCode != value)
            {
                Client.PostalCode = value;
                OnPropertyChanged();
            }
        }
    }

    public string? Country
    {
        get => Client.Country;
        set
        {
            if (Client.Country != value)
            {
                Client.Country = value;
                OnPropertyChanged();
            }
        }
    }

    public string? PaymentTerms
    {
        get => Client.PaymentTerms;
        set
        {
            if (Client.PaymentTerms != value)
            {
                Client.PaymentTerms = value;
                OnPropertyChanged();
            }
        }
    }

    public decimal? CreditLimit
    {
        get => Client.CreditLimit;
        set
        {
            if (Client.CreditLimit != value)
            {
                Client.CreditLimit = value;
                OnPropertyChanged();
            }
        }
    }

    public CurrencyCode SelectedCurrency
    {
        get => Client.DefaultCurrency;
        set
        {
            if (Client.DefaultCurrency != value)
            {
                Client.DefaultCurrency = value;
                OnPropertyChanged();
            }
        }
    }

    public string? Notes
    {
        get => Client.Notes;
        set
        {
            if (Client.Notes != value)
            {
                Client.Notes = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsActive
    {
        get => Client.IsActive;
        set
        {
            if (Client.IsActive != value)
            {
                Client.IsActive = value;
                OnPropertyChanged();
            }
        }
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    #endregion

    #region Events

    public event EventHandler<Client>? SaveCompleted;
    public event EventHandler? CancelRequested;

    #endregion

    #region Methods

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(CompanyName);
    }

    private void Save(object? parameter)
    {
        if (!CanSave()) return;

        Client.UpdatedAt = DateTime.UtcNow;
        if (_isNew)
        {
            Client.CreatedAt = DateTime.UtcNow;
            // Generate client code if empty
            if (string.IsNullOrWhiteSpace(Client.ClientCode))
            {
                Client.ClientCode = $"CLI-{DateTime.Now:yyyyMMddHHmmss}";
            }
        }

        SaveCompleted?.Invoke(this, Client);
    }

    private void Cancel(object? parameter)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}
