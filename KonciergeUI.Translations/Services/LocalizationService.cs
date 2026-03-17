using System.Globalization;
using KonciergeUI.Translations.Resources;
using Microsoft.Extensions.Localization;

namespace KonciergeUI.Translations.Services;

public class LocalizationService:ILocalizationService
{
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

    public string SetCulture(string cultureName)
    {
        var culture = ResolveCulture(cultureName);
        _currentCulture = culture;

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        CultureChanged?.Invoke(this, culture);
        return culture.Name;
    }

    private static CultureInfo ResolveCulture(string requestedCultureName)
    {
        var normalized = requestedCultureName.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = "en";
        }

        foreach (var candidate in GetCultureCandidates(normalized))
        {
            try
            {
                return new CultureInfo(candidate);
            }
            catch (CultureNotFoundException)
            {
                // Try next candidate.
            }
        }

        return new CultureInfo("en");
    }

    private static IEnumerable<string> GetCultureCandidates(string normalizedCulture)
    {
        yield return normalizedCulture;

        // Some Windows environments don't resolve neutral "lij", while "lij-IT" is available.
        if (string.Equals(normalizedCulture, "lij", StringComparison.OrdinalIgnoreCase))
        {
            yield return "lij-IT";
        }

        if (!string.Equals(normalizedCulture, "en", StringComparison.OrdinalIgnoreCase))
        {
            yield return "en";
        }
    }


    public event EventHandler<CultureInfo>? CultureChanged;
}