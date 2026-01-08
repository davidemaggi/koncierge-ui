using KonciergeUI.Models.Forwarding;
using KonciergeUI.Models.Kube;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KonciergeUi.Client.State;

public class UiState : INotifyPropertyChanged
{
    private ClusterConnectionInfo? _selectedCluster;
    private ForwardTemplate? _selectedTemplate;
    private string _currentTheme = "System"; // System, Light, Dark
    private string _currentLanguage = "en";

    public ClusterConnectionInfo? SelectedCluster
    {
        get => _selectedCluster;
        set
        {
            if (_selectedCluster != value)
            {
                _selectedCluster = value;
                OnPropertyChanged();
            }
        }
    }

    public ForwardTemplate? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            if (_selectedTemplate != value)
            {
                _selectedTemplate = value;
                OnPropertyChanged();
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
}