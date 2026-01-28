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
        IStringLocalizer<Navigation> navigation,
        IStringLocalizer<Enumerations> enums,
        IStringLocalizer<Templates> templates,
        IStringLocalizer<Forwards> forwards,
        IStringLocalizer<Resources.Resources> resources,
        IStringLocalizer<Global> global,
        IStringLocalizer<Errors> errors
        //IStringLocalizer<Settings> settings
        )
    {
        Navigation = navigation;
        Enumerations = enums;
        Templates = templates;
        Forwards = forwards;
        Resources = resources;
        Global = global;
        Errors = errors;

        _currentCulture = CultureInfo.CurrentCulture;
    }

    public IStringLocalizer Navigation { get; }
    public IStringLocalizer Enumerations { get; }
    public IStringLocalizer Templates { get; }
    public IStringLocalizer Forwards { get; }
    public IStringLocalizer Resources { get; } 
    public IStringLocalizer Global { get; } 
    public IStringLocalizer Errors { get; } 

    public CultureInfo CurrentCulture => _currentCulture;

    public void SetCulture(string cultureName)
    {
        var culture = new CultureInfo(cultureName);
        _currentCulture = culture;

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        CultureChanged?.Invoke(this, culture);
    }


    public event EventHandler<CultureInfo>? CultureChanged;
}