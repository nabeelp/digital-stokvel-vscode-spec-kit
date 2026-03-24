using System.Text.Json;
using DigitalStokvel.Core.Interfaces;

namespace DigitalStokvel.Services;

/// <summary>
/// Service for managing multilingual content from JSON resource files
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly Dictionary<string, Dictionary<string, string>> _localizations = new();
    private readonly string[] _supportedLanguages = { "en", "zu", "st", "xh", "af" };
    private readonly string _resourcePath;

    public LocalizationService(string? resourcePath = null)
    {
        _resourcePath = resourcePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "localization");
        LoadLocalizations();
    }

    public string GetString(string key, string languageCode, params object[] parameters)
    {
        if (!IsLanguageSupported(languageCode))
        {
            languageCode = "en"; // Fallback to English
        }

        if (_localizations.TryGetValue(languageCode, out var strings) &&
            strings.TryGetValue(key, out var value))
        {
            try
            {
                return parameters.Length > 0 ? string.Format(value, parameters) : value;
            }
            catch (FormatException)
            {
                return value; // Return raw value if formatting fails
            }
        }

        // Return key if translation not found (helps identify missing translations)
        return key;
    }

    public Dictionary<string, string> GetAllStrings(string languageCode)
    {
        if (!IsLanguageSupported(languageCode))
        {
            languageCode = "en";
        }

        return _localizations.TryGetValue(languageCode, out var strings)
            ? new Dictionary<string, string>(strings)
            : new Dictionary<string, string>();
    }

    public bool IsLanguageSupported(string languageCode)
    {
        return _supportedLanguages.Contains(languageCode?.ToLowerInvariant());
    }

    public string[] GetSupportedLanguages()
    {
        return (string[])_supportedLanguages.Clone();
    }

    private void LoadLocalizations()
    {
        foreach (var lang in _supportedLanguages)
        {
            var filePath = Path.Combine(_resourcePath, $"{lang}.json");

            if (File.Exists(filePath))
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    var strings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                    if (strings != null)
                    {
                        _localizations[lang] = strings;
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't fail - fall back to English
                    Console.WriteLine($"Failed to load localization file for {lang}: {ex.Message}");
                    _localizations[lang] = new Dictionary<string, string>();
                }
            }
            else
            {
                // Initialize empty dictionary if file doesn't exist
                _localizations[lang] = new Dictionary<string, string>();
            }
        }
    }
}
