using KonciergeUI.Models.Forwarding;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KonciergeUi.Client.State;

public class ForwardSessionState : INotifyPropertyChanged
{
    private ObservableCollection<ForwardTemplateExecution> _activeExecutions = new();
    private Dictionary<string, List<string>> _forwardLogs = new();

    public ObservableCollection<ForwardTemplateExecution> ActiveExecutions
    {
        get => _activeExecutions;
        set
        {
            _activeExecutions = value;
            OnPropertyChanged();
        }
    }

    public void AddLogEntry(string forwardId, string logMessage)
    {
        if (!_forwardLogs.ContainsKey(forwardId))
        {
            _forwardLogs[forwardId] = new List<string>();
        }
        
        _forwardLogs[forwardId].Add($"[{DateTime.Now:HH:mm:ss}] {logMessage}");
        LogEntryAdded?.Invoke(this, forwardId);
    }

    public List<string> GetLogs(string forwardId)
    {
        return _forwardLogs.TryGetValue(forwardId, out var logs) 
            ? logs 
            : new List<string>();
    }

    public void ClearLogs(string forwardId)
    {
        if (_forwardLogs.ContainsKey(forwardId))
        {
            _forwardLogs[forwardId].Clear();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<string>? LogEntryAdded;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}