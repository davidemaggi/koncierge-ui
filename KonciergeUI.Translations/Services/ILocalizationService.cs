using System.Globalization;
using Microsoft.Extensions.Localization;

namespace KonciergeUI.Translations.Services;

public interface ILocalizationService
{
    
    IStringLocalizer Navigation { get; }
    //IStringLocalizer Dashboard { get; }
    //IStringLocalizer Templates { get; }
    //IStringLocalizer Forwards { get; }
    //IStringLocalizer Settings { get; }
    CultureInfo CurrentCulture { get; }
    void SetCulture(string cultureName);
    event EventHandler<CultureInfo>? CultureChanged;
}