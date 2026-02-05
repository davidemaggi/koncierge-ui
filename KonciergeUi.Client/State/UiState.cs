using KonciergeUI.Models.Forwarding;
using KonciergeUI.Models.Kube;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using KonciergeUI.Data;
using KonciergeUI.Translations.Services;
using KonciergeUI.Models.Security;
using System.Collections.Generic;
using System.Linq;

namespace KonciergeUi.Client.State;

public class UiState : INotifyPropertyChanged
{
    private readonly IPreferencesStorage _preferencesStorage;
    private readonly ILocalizationService _localizationService;
    private ClusterConnectionInfo? _selectedCluster;
    private string _currentTheme = "System";
    private string _currentLanguage = "en";

    private string? _selectedNamespace = null;
    private string? _selectedType = null;
    private string? _selectedStatus = null;
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
                _localizationService.SetCulture(value);
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

        _localizationService.SetCulture(_currentLanguage);
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
