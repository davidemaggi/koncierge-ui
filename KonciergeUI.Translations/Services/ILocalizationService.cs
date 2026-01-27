using KonciergeUI.Translations.Resources;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace KonciergeUI.Translations.Services;

public interface ILocalizationService
{
    
    IStringLocalizer Navigation { get; }
    IStringLocalizer Enumerations { get; }
    //IStringLocalizer Dashboard { get; }
    //IStringLocalizer Templates { get; }
    //IStringLocalizer Forwards { get; }
    //IStringLocalizer Settings { get; }
    CultureInfo CurrentCulture { get; }
    void SetCulture(string cultureName);
    event EventHandler<CultureInfo>? CultureChanged;
}