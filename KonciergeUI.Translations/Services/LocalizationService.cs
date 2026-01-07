using System.Globalization;
using KonciergeUI.Translations.Resources;
using Microsoft.Extensions.Localization;
using Microsoft.VisualBasic;

namespace KonciergeUI.Translations.Services;

public class LocalizationService:ILocalizationService
{
    private readonly IStringLocalizer<Strings> _localizer;
    private CultureInfo _currentCulture;
    
    
    public LocalizationService(
        IStringLocalizer<Navigation> navigation//,
        //IStringLocalizer<Dashboard> dashboard,
        //IStringLocalizer<Templates> templates,
        //IStringLocalizer<Forwards> forwards,
        //IStringLocalizer<Settings> settings
        )
    {
        Navigation = navigation;
        //Dashboard = dashboard;
        //Templates = templates;
        //Forwards = forwards;
        //Settings = settings;

        _currentCulture = CultureInfo.CurrentCulture;
    }

    public IStringLocalizer Navigation { get; }
    public IStringLocalizer Dashboard { get; }
    public IStringLocalizer Templates { get; }
    public IStringLocalizer Forwards { get; }
    public IStringLocalizer Settings { get; }

    public CultureInfo CurrentCulture => _currentCulture;

    public void SetCulture(string cultureName)
    {
        var culture = new CultureInfo(cultureName);
        _currentCulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        
        CultureChanged?.Invoke(this, culture);
    }

    public event EventHandler<CultureInfo>? CultureChanged;
}