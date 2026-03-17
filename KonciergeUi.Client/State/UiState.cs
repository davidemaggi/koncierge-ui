using KonciergeUI.Models.Forwarding;
using KonciergeUI.Models.Kube;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using KonciergeUI.Data;
using KonciergeUI.Translations.Services;
using KonciergeUI.Models.Security;
using KonciergeUI.Models;
using KonciergeUI.Core.Abstractions;

namespace KonciergeUi.Client.State;

public class UiState : INotifyPropertyChanged
{
    private readonly IPreferencesStorage _preferencesStorage;
    private readonly ILocalizationService _localizationService;
    private readonly IClusterDiscoveryService _clusterDiscoveryService;
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
    private bool _isEditingTemplate;
    private bool _hasUnsavedTemplateChanges;
    private string _templatesFilterText = string.Empty;
    private IEnumerable<string> _templatesFilterTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private bool _templatesMatchAllTags;
    private string _activeForwardsFilterText = string.Empty;
    private IEnumerable<string> _activeForwardsFilterTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private bool _activeForwardsMatchAllTags;
    private IEnumerable<string> _activeForwardsFilterClusterIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private TimeSpan _activeForwardsFastRefreshInterval = TimeSpan.FromMilliseconds(500);
    private TimeSpan _activeForwardsSlowRefreshInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _activeForwardsFastRefreshWindow = TimeSpan.FromSeconds(5);

    public UiState(IPreferencesStorage preferencesStorage, ILocalizationService localizationService, IClusterDiscoveryService clusterDiscoveryService)
    {
        _preferencesStorage = preferencesStorage;
        _localizationService = localizationService;
        _clusterDiscoveryService = clusterDiscoveryService;
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
            if (string.Equals(_currentLanguage, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var appliedCulture = _localizationService.SetCulture(value);

            if (_currentLanguage != appliedCulture)
            {
                _currentLanguage = appliedCulture;
                OnPropertyChanged();
                LanguageChanged?.Invoke(this, appliedCulture);

                _config.CurrentLanguage = appliedCulture;
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

        _currentLanguage = _localizationService.SetCulture(_currentLanguage);

        if (!string.IsNullOrWhiteSpace(_config.LastSelectedClusterId))
        {
            try
            {
                var cluster = await _clusterDiscoveryService.GetClusterByIdAsync(_config.LastSelectedClusterId);
                if (cluster is not null)
                {
                    SelectedCluster = cluster;
                }
            }
            catch (Exception)
            {
                // Leave SelectedCluster unchanged if lookup fails.
            }
        }
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

    public bool IsEditingTemplate
    {
        get => _isEditingTemplate;
        set
        {
            if (_isEditingTemplate == value)
            {
                return;
            }

            _isEditingTemplate = value;
            OnPropertyChanged();
        }
    }

    public bool HasUnsavedTemplateChanges
    {
        get => _hasUnsavedTemplateChanges;
        set
        {
            if (_hasUnsavedTemplateChanges == value)
            {
                return;
            }

            _hasUnsavedTemplateChanges = value;
            OnPropertyChanged();
        }
    }

    public string TemplatesFilterText
    {
        get => _templatesFilterText;
        set
        {
            var normalized = value.Trim();
            if (_templatesFilterText == normalized)
            {
                return;
            }

            _templatesFilterText = normalized;
            OnPropertyChanged();
        }
    }

    public IEnumerable<string> TemplatesFilterTags
    {
        get => _templatesFilterTags;
        set
        {
            var normalized = NormalizeFilterValues(value);
            if (SetEqualsCaseInsensitive(_templatesFilterTags, normalized))
            {
                return;
            }

            _templatesFilterTags = normalized;
            OnPropertyChanged();
        }
    }

    public bool TemplatesMatchAllTags
    {
        get => _templatesMatchAllTags;
        set
        {
            if (_templatesMatchAllTags == value)
            {
                return;
            }

            _templatesMatchAllTags = value;
            OnPropertyChanged();
        }
    }

    public string ActiveForwardsFilterText
    {
        get => _activeForwardsFilterText;
        set
        {
            var normalized = value.Trim();
            if (_activeForwardsFilterText == normalized)
            {
                return;
            }

            _activeForwardsFilterText = normalized;
            OnPropertyChanged();
        }
    }

    public IEnumerable<string> ActiveForwardsFilterTags
    {
        get => _activeForwardsFilterTags;
        set
        {
            var normalized = NormalizeFilterValues(value);
            if (SetEqualsCaseInsensitive(_activeForwardsFilterTags, normalized))
            {
                return;
            }

            _activeForwardsFilterTags = normalized;
            OnPropertyChanged();
        }
    }

    public bool ActiveForwardsMatchAllTags
    {
        get => _activeForwardsMatchAllTags;
        set
        {
            if (_activeForwardsMatchAllTags == value)
            {
                return;
            }

            _activeForwardsMatchAllTags = value;
            OnPropertyChanged();
        }
    }

    public IEnumerable<string> ActiveForwardsFilterClusterIds
    {
        get => _activeForwardsFilterClusterIds;
        set
        {
            var normalized = NormalizeFilterValues(value);
            if (SetEqualsCaseInsensitive(_activeForwardsFilterClusterIds, normalized))
            {
                return;
            }

            _activeForwardsFilterClusterIds = normalized;
            OnPropertyChanged();
        }
    }

    public TimeSpan ActiveForwardsFastRefreshInterval
    {
        get => _activeForwardsFastRefreshInterval;
        set
        {
            var normalized = value <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(500) : value;
            if (_activeForwardsFastRefreshInterval == normalized)
            {
                return;
            }

            _activeForwardsFastRefreshInterval = normalized;
            OnPropertyChanged();
        }
    }

    public TimeSpan ActiveForwardsSlowRefreshInterval
    {
        get => _activeForwardsSlowRefreshInterval;
        set
        {
            var normalized = value <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : value;
            if (_activeForwardsSlowRefreshInterval == normalized)
            {
                return;
            }

            _activeForwardsSlowRefreshInterval = normalized;
            OnPropertyChanged();
        }
    }

    public TimeSpan ActiveForwardsFastRefreshWindow
    {
        get => _activeForwardsFastRefreshWindow;
        set
        {
            var normalized = value < TimeSpan.Zero ? TimeSpan.Zero : value;
            if (_activeForwardsFastRefreshWindow == normalized)
            {
                return;
            }

            _activeForwardsFastRefreshWindow = normalized;
            OnPropertyChanged();
        }
    }

    public void SetTemplateDraft(ForwardTemplate template)
    {
        TemplateDraft = CloneTemplate(template);
        IsEditingTemplate = true;
        HasUnsavedTemplateChanges = false;
    }

    public ForwardTemplate GetOrCreateTemplateDraft()
    {
        if (TemplateDraft is null)
        {
            TemplateDraft = CreateDefaultDraft();
            IsEditingTemplate = false;
            HasUnsavedTemplateChanges = false;
        }
        else if (string.IsNullOrWhiteSpace(TemplateDraft.Name))
        {
            TemplateDraft = TemplateDraft with { Name = "Draft" };
        }

        return TemplateDraft;
    }

    public void UpdateTemplateDraft(ForwardTemplate template)
    {
        TemplateDraft = CloneTemplate(template);
        HasUnsavedTemplateChanges = true;
    }

    public void ClearTemplateDraft()
    {
        TemplateDraft = CreateDefaultDraft();
        IsEditingTemplate = false;
        HasUnsavedTemplateChanges = false;
    }

    public ForwardTemplate? ConsumeTemplateDraft()
    {
        var draft = TemplateDraft;
        TemplateDraft = null;
        IsEditingTemplate = false;
        HasUnsavedTemplateChanges = false;
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

    private static ForwardTemplate CreateDefaultDraft()
    {
        return new ForwardTemplate { Name = "Draft" };
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

    private static List<string> NormalizeFilterValues(IEnumerable<string>? values)
    {
        return (values ?? Enumerable.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool SetEqualsCaseInsensitive(IEnumerable<string> left, IEnumerable<string> right)
    {
        var leftSet = new HashSet<string>(left, StringComparer.OrdinalIgnoreCase);
        var rightSet = new HashSet<string>(right, StringComparer.OrdinalIgnoreCase);
        return leftSet.SetEquals(rightSet);
    }

}
