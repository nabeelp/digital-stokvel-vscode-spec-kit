namespace DigitalStokvel.Core.Interfaces;

/// <summary>
/// Service for managing multilingual content across 5 languages.
/// Supports: English (en), isiZulu (zu), Sesotho (st), Xhosa (xh), Afrikaans (af)
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets a localized string by key for the specified language
    /// </summary>
    /// <param name="key">Resource key (e.g., "payment.reminder.message")</param>
    /// <param name="languageCode">Language code (en, zu, st, xh, af)</param>
    /// <param name="parameters">Optional parameters for string interpolation</param>
    /// <returns>Localized string, or key if translation not found</returns>
    string GetString(string key, string languageCode, params object[] parameters);

    /// <summary>
    /// Gets all strings for a specific language
    /// </summary>
    /// <param name="languageCode">Language code (en, zu, st, xh, af)</param>
    /// <returns>Dictionary of key-value pairs for the language</returns>
    Dictionary<string, string> GetAllStrings(string languageCode);

    /// <summary>
    /// Checks if a language code is supported
    /// </summary>
    /// <param name="languageCode">Language code to check</param>
    /// <returns>True if supported, false otherwise</returns>
    bool IsLanguageSupported(string languageCode);

    /// <summary>
    /// Gets the list of all supported language codes
    /// </summary>
    /// <returns>Array of language codes: ["en", "zu", "st", "xh", "af"]</returns>
    string[] GetSupportedLanguages();
}
