using KonciergeUI.Models.Forwarding;
using KonciergeUI.Models.Kube;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using KonciergeUI.Data;
using KonciergeUI.Translations.Services;
using KonciergeUI.Models.Security;
using System.Collections.Generic;
using System.Linq;
using KonciergeUI.Models;

namespace KonciergeUi.Client.State;

public class UiState : INotifyPropertyChanged
{
    private readonly IPreferencesStorage _preferencesStorage;
    private readonly ILocalizationService _localizationService;
    private ClusterConnectionInfo? _selectedCluster;
    private string _currentTheme = "System";
    private string _currentLanguage = "en";
    private KonciergeConfig _config = new();
    private bool _isDarkMode;

    private IEnumerable<string> _selectedNamespaces = new HashSet<string>();
    private IEnumerable<string> _selectedTypes = new HashSet<string>();
    private IEnumerable<string> _selectedStatuses = new HashSet<string>();
    private string? _searchString = null;
    private ForwardTemplate? _templateDraft;

    public UiState(IPreferencesStorage preferencesStorage, ILocalizationService localizationService)
    {
        _preferencesStorage = preferencesStorage;
        _localizationService = localizationService;
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

                if (value != null)
                {
                    _config.LastSelectedClusterId = value.Id;
                    _ = _preferencesStorage.UpdateConfigAsync(_config);
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

                _config.CurrentTheme = value;
                _ = _preferencesStorage.UpdateConfigAsync(_config);
            }
        }
    }

    /// <summary>
    /// The actual dark mode state (resolved from CurrentTheme and system preference).
    /// </summary>
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode != value)
            {
                _isDarkMode = value;
                OnPropertyChanged();
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
                _localizationService.SetCulture(value);
                OnPropertyChanged();
                LanguageChanged?.Invoke(this, value);

                _config.CurrentLanguage = value;
                _ = _preferencesStorage.UpdateConfigAsync(_config);
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
        _config = await _preferencesStorage.GetConfigAsync();

        if (!string.IsNullOrEmpty(_config.CurrentTheme))
        {
            _currentTheme = _config.CurrentTheme;
        }

        if (!string.IsNullOrEmpty(_config.CurrentLanguage))
        {
            _currentLanguage = _config.CurrentLanguage;
        }

        _localizationService.SetCulture(_currentLanguage);
    }

    public Task<string?> GetLastSelectedClusterIdAsync()
    {
        return Task.FromResult(_config.LastSelectedClusterId);
    }


    public IEnumerable<string> SelectedTypes
    {
        get => _selectedTypes;
        set
        {
            if (!_selectedTypes.SequenceEqual(value))
            {
                _selectedTypes = value;
                OnPropertyChanged();
            }
        }
    }

    public IEnumerable<string> SelectedNamespaces
    {
        get => _selectedNamespaces;
        set
        {
            if (!_selectedNamespaces.SequenceEqual(value))
            {
                _selectedNamespaces = value;
                OnPropertyChanged();
            }
        }
    }

    public IEnumerable<string> SelectedStatuses
    {
        get => _selectedStatuses;
        set
        {
            if (!_selectedStatuses.SequenceEqual(value))
            {
                _selectedStatuses = value;
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

    public void SetTemplateDraft(ForwardTemplate template)
    {
        TemplateDraft = CloneTemplate(template);
    }

    public ForwardTemplate? ConsumeTemplateDraft()
    {
        var draft = TemplateDraft;
        TemplateDraft = null;
        return draft;
    }

    public ForwardTemplate? TemplateDraft
    {
        get => _templateDraft;
        private set
        {
            if (_templateDraft == value)
            {
                return;
            }

            _templateDraft = value;
            OnPropertyChanged();
        }
    }

    private static ForwardTemplate CloneTemplate(ForwardTemplate template)
    {
        return template with
        {
            Tags = template.Tags is null ? null : new List<string>(template.Tags),
            Forwards = template.Forwards
                .Select(fwd => fwd with
                {
                    LinkedSecrets = fwd.LinkedSecrets is null
                        ? new List<SecretReference>()
                        : new List<SecretReference>(fwd.LinkedSecrets)
                })
                .ToList()
        };
    }

}
