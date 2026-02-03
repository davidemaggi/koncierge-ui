using Microsoft.Maui.ApplicationModel;

namespace KonciergeUi.Client.Services;

public interface IAppVersionProvider
{
    string DisplayVersion { get; }
    string BuildVersion { get; }
}

public class AppVersionProvider : IAppVersionProvider
{
    public string DisplayVersion => AppInfo.Current.VersionString;
    public string BuildVersion => AppInfo.Current.BuildString;
}
