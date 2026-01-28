using KonciergeUI.Models.Forwarding;
using KonciergeUI.Models.Kube;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using KonciergeUI.Data;

namespace KonciergeUi.Client.State;

public class UiState : INotifyPropertyChanged
{
    private readonly IPreferencesStorage _preferencesStorage;
    private ClusterConnectionInfo? _selectedCluster;
    private string _currentTheme = "System";
    private string _currentLanguage = "en";

    private string? _selectedNamespace = null;
    private string? _selectedType = null;
    private string? _selectedStatus = null;
    private string? _searchString = null;
    

    public UiState(IPreferencesStorage preferencesStorage)
    {
        _preferencesStorage = preferencesStorage;
    }

    public ClusterConnectionInfo? SelectedCluster
    {
        get => _selectedCluster;
        set
        {
            if (_selectedCluster != value)
            {
                _selectedCluster = value;
                OnPropertyChanged();

                // Persist to storage
                if (value != null)
                {
                    _ = _preferencesStorage.SetLastSelectedClusterIdAsync(value.Id);
                }
            }
        }
    }

    public string CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                OnPropertyChanged();
                ThemeChanged?.Invoke(this, value);

                // Persist to storage
                _ = _preferencesStorage.SetCurrentThemeAsync(value);
            }
        }
    }

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                OnPropertyChanged();
                LanguageChanged?.Invoke(this, value);

                // Persist to storage
                _ = _preferencesStorage.SetCurrentLanguageAsync(value);
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<string>? ThemeChanged;
    public event EventHandler<string>? LanguageChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Load saved preferences on startup
    public async Task LoadPreferencesAsync()
    {
        var theme = await _preferencesStorage.GetCurrentThemeAsync();
        if (!string.IsNullOrEmpty(theme))
        {
            _currentTheme = theme;
        }

        var language = await _preferencesStorage.GetCurrentLanguageAsync();
        if (!string.IsNullOrEmpty(language))
        {
            _currentLanguage = language;
        }
    }

    public async Task<string?> GetLastSelectedClusterIdAsync()
    {
        return await _preferencesStorage.GetLastSelectedClusterIdAsync();
    }


    public string? SelectedType
    {
        get => _selectedType;
        set
        {
            if (_selectedType != value)
            {
                _selectedType = value;
                OnPropertyChanged();

                
            }
        }
    }

    public string? SelectedNamespace
    {
        get => _selectedNamespace;
        set
        {
            if (_selectedNamespace != value)
            {
                _selectedNamespace = value;
                OnPropertyChanged();


            }
        }
    }
    public string? SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (_selectedStatus != value)
            {
                _selectedStatus = value;
                OnPropertyChanged();


            }
        }
    }
    public string? SearchString
    {
        get => _searchString;
        set
        {
            if (_searchString != value)
            {
                _searchString = value;
                OnPropertyChanged();


            }
        }
    }

}