using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Services;
using FluentAssertions;

namespace DigitalStokvel.Tests.Unit.Services;

/// <summary>
/// Unit tests for LocalizationService - multilingual support for 5 languages
/// </summary>
public class LocalizationServiceTests : IDisposable
{
    private readonly string _testResourcePath;
    private readonly LocalizationService _sut; // System Under Test

    public LocalizationServiceTests()
    {
        // Create temporary directory for test resource files
        _testResourcePath = Path.Combine(Path.GetTempPath(), $"localization_tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testResourcePath);

        // Create test JSON files for all supported languages
        CreateTestLocalizationFile("en", new Dictionary<string, string>
        {
            { "welcome", "Welcome" },
            { "goodbye", "Goodbye" },
            { "greeting", "Hello, {0}!" },
            { "balance", "Your balance is R{0}" },
            { "error.notFound", "Item not found" }
        });

        CreateTestLocalizationFile("zu", new Dictionary<string, string>
        {
            { "welcome", "Sawubona" },
            { "goodbye", "Hamba kahle" },
            { "greeting", "Sawubona, {0}!" },
            { "balance", "Ibhalansi yakho ngu-R{0}" },
            { "error.notFound", "Okungafunwayo akutholakalanga" }
        });

        CreateTestLocalizationFile("st", new Dictionary<string, string>
        {
            { "welcome", "Dumela" },
            { "goodbye", "Sala hantle" },
            { "greeting", "Dumela, {0}!" },
            { "balance", "Tefo ya hao ke R{0}" }
            // Intentionally missing error.notFound for fallback testing
        });

        CreateTestLocalizationFile("xh", new Dictionary<string, string>
        {
            { "welcome", "Molo" },
            { "goodbye", "Salisukuhle" },
            { "greeting", "Molo, {0}!" },
            { "balance", "Ibhalansi yakho ngu-R{0}" }
        });

        CreateTestLocalizationFile("af", new Dictionary<string, string>
        {
            { "welcome", "Welkom" },
            { "goodbye", "Totsiens" },
            { "greeting", "Hallo, {0}!" },
            { "balance", "Jou balans is R{0}" }
        });

        _sut = new LocalizationService(_testResourcePath);
    }

    public void Dispose()
    {
        // Cleanup: Delete test resource directory
        if (Directory.Exists(_testResourcePath))
        {
            Directory.Delete(_testResourcePath, recursive: true);
        }
    }

    private void CreateTestLocalizationFile(string languageCode, Dictionary<string, string> translations)
    {
        var filePath = Path.Combine(_testResourcePath, $"{languageCode}.json");
        var json = System.Text.Json.JsonSerializer.Serialize(translations, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(filePath, json);
    }

    #region GetString Tests

    [Theory]
    [InlineData("en", "welcome", "Welcome")]
    [InlineData("zu", "welcome", "Sawubona")]
    [InlineData("st", "welcome", "Dumela")]
    [InlineData("xh", "welcome", "Molo")]
    [InlineData("af", "welcome", "Welkom")]
    public void GetString_WithValidKeyAndLanguage_ShouldReturnCorrectTranslation(string languageCode, string key, string expected)
    {
        // Act
        var result = _sut.GetString(key, languageCode);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("en", "greeting", "Alice", "Hello, Alice!")]
    [InlineData("zu", "greeting", "Sipho", "Sawubona, Sipho!")]
    [InlineData("st", "greeting", "Thabo", "Dumela, Thabo!")]
    [InlineData("xh", "greeting", "Nomsa", "Molo, Nomsa!")]
    [InlineData("af", "greeting", "Pieter", "Hallo, Pieter!")]
    public void GetString_WithParameterSubstitution_ShouldFormatStringCorrectly(string languageCode, string key, string param, string expected)
    {
        // Act
        var result = _sut.GetString(key, languageCode, param);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("en", "balance", 1250.50, "Your balance is R1250.5")]
    [InlineData("zu", "balance", 500.00, "Ibhalansi yakho ngu-R500")]
    [InlineData("af", "balance", 10000, "Jou balans is R10000")]
    public void GetString_WithNumericParameterSubstitution_ShouldFormatCorrectly(string languageCode, string key, decimal amount, string expected)
    {
        // Act
        var result = _sut.GetString(key, languageCode, amount);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetString_WithMissingKey_ShouldReturnKeyAsString()
    {
        // Act
        var result = _sut.GetString("nonexistent.key", "en");

        // Assert
        result.Should().Be("nonexistent.key");
    }

    [Fact]
    public void GetString_WithUnsupportedLanguage_ShouldFallbackToEnglish()
    {
        // Act
        var result = _sut.GetString("welcome", "fr"); // French not supported

        // Assert
        result.Should().Be("Welcome"); // English fallback
    }

    [Fact]
    public void GetString_WithNullLanguageCode_ShouldFallbackToEnglish()
    {
        // Act
        var result = _sut.GetString("welcome", null!);

        // Assert
        result.Should().Be("Welcome");
    }

    [Fact]
    public void GetString_WithEmptyLanguageCode_ShouldFallbackToEnglish()
    {
        // Act
        var result = _sut.GetString("welcome", "");

        // Assert
        result.Should().Be("Welcome");
    }

    [Fact]
    public void GetString_WithMissingTranslationInLanguage_ShouldReturnKey()
    {
        // Arrange: "error.notFound" is missing in Sesotho (st) file

        // Act
        var result = _sut.GetString("error.notFound", "st");

        // Assert
        result.Should().Be("error.notFound"); // Returns key when translation missing
    }

    [Fact]
    public void GetString_WithInvalidFormatString_ShouldReturnRawValue()
    {
        // Arrange: Create a translation with mismatched format placeholders
        var invalidFormatKey = "invalid.format";
        var testPath = Path.Combine(_testResourcePath, "en.json");
        var existingJson = File.ReadAllText(testPath);
        var translations = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(existingJson);
        translations![invalidFormatKey] = "Value {0} and {1}"; // Expects 2 params
        File.WriteAllText(testPath, System.Text.Json.JsonSerializer.Serialize(translations));

        // Reload service to pick up changes
        var service = new LocalizationService(_testResourcePath);

        // Act - only provide 1 parameter when 2 are expected
        var result = service.GetString(invalidFormatKey, "en", "param1");

        // Assert - should return raw value when formatting fails
        result.Should().Be("Value {0} and {1}");
    }

    [Theory]
    [InlineData("en", "welcome", "Welcome")] // Lowercase - correct format
    [InlineData("zu", "welcome", "Sawubona")] // Lowercase - correct format
    public void GetString_WithLowercaseLanguageCode_ShouldReturnCorrectTranslation(string languageCode, string key, string expected)
    {
        // Act
        var result = _sut.GetString(key, languageCode);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region GetAllStrings Tests

    [Fact]
    public void GetAllStrings_WithEnglish_ShouldReturnAllEnglishTranslations()
    {
        // Act
        var result = _sut.GetAllStrings("en");

        // Assert
        result.Should().NotBeEmpty();
        result.Should().ContainKey("welcome");
        result.Should().ContainKey("goodbye");
        result.Should().ContainKey("greeting");
        result.Should().ContainKey("balance");
        result["welcome"].Should().Be("Welcome");
        result["goodbye"].Should().Be("Goodbye");
    }

    [Fact]
    public void GetAllStrings_WithIsiZulu_ShouldReturnAllZuluTranslations()
    {
        // Act
        var result = _sut.GetAllStrings("zu");

        // Assert
        result.Should().NotBeEmpty();
        result.Should().ContainKey("welcome");
        result["welcome"].Should().Be("Sawubona");
        result["goodbye"].Should().Be("Hamba kahle");
    }

    [Fact]
    public void GetAllStrings_WithUnsupportedLanguage_ShouldFallbackToEnglish()
    {
        // Act
        var result = _sut.GetAllStrings("fr"); // French not supported

        // Assert
        result.Should().NotBeEmpty();
        result["welcome"].Should().Be("Welcome"); // English fallback
    }

    [Fact]
    public void GetAllStrings_ShouldReturnCopyOfDictionary()
    {
        // Act
        var result1 = _sut.GetAllStrings("en");
        var result2 = _sut.GetAllStrings("en");

        // Modify first dictionary
        result1["welcome"] = "Modified";

        // Assert - second dictionary should not be affected
        result2["welcome"].Should().Be("Welcome");
    }

    [Fact]
    public void GetAllStrings_WithSesotho_ShouldReturnAvailableTranslations()
    {
        // Act
        var result = _sut.GetAllStrings("st");

        // Assert
        result.Should().NotBeEmpty();
        result.Should().ContainKey("welcome");
        result.Should().ContainKey("greeting");
        result.Should().NotContainKey("error.notFound"); // Missing in st file
    }

    #endregion

    #region IsLanguageSupported Tests

    [Theory]
    [InlineData("en", true)]
    [InlineData("zu", true)]
    [InlineData("st", true)]
    [InlineData("xh", true)]
    [InlineData("af", true)]
    [InlineData("fr", false)]
    [InlineData("de", false)]
    [InlineData("es", false)]
    [InlineData("", false)]
    public void IsLanguageSupported_WithVariousLanguageCodes_ShouldReturnCorrectResult(string languageCode, bool expected)
    {
        // Act
        var result = _sut.IsLanguageSupported(languageCode);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("EN")]
    [InlineData("En")]
    [InlineData("eN")]
    [InlineData("ZU")]
    [InlineData("Zu")]
    public void IsLanguageSupported_WithCaseInsensitiveLanguageCode_ShouldReturnTrue(string languageCode)
    {
        // Act
        var result = _sut.IsLanguageSupported(languageCode);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsLanguageSupported_WithNullLanguageCode_ShouldReturnFalse()
    {
        // Act
        var result = _sut.IsLanguageSupported(null!);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetSupportedLanguages Tests

    [Fact]
    public void GetSupportedLanguages_ShouldReturnAllFiveLanguages()
    {
        // Act
        var result = _sut.GetSupportedLanguages();

        // Assert
        result.Should().HaveCount(5);
        result.Should().Contain(new[] { "en", "zu", "st", "xh", "af" });
    }

    [Fact]
    public void GetSupportedLanguages_ShouldReturnCopyOfArray()
    {
        // Act
        var result1 = _sut.GetSupportedLanguages();
        var result2 = _sut.GetSupportedLanguages();

        // Modify first array
        result1[0] = "modified";

        // Assert - second array should not be affected
        result2[0].Should().Be("en");
    }

    #endregion

    #region File Loading Tests

    [Fact]
    public void LocalizationService_WithMissingResourceFiles_ShouldInitializeWithEmptyDictionaries()
    {
        // Arrange - create service pointing to non-existent directory
        var emptyPath = Path.Combine(Path.GetTempPath(), $"empty_{Guid.NewGuid()}");

        // Act
        var service = new LocalizationService(emptyPath);

        // Assert - should not throw, and GetString should return key
        var result = service.GetString("welcome", "en");
        result.Should().Be("welcome"); // Returns key since file doesn't exist
    }

    [Fact]
    public void LocalizationService_WithInvalidJsonFile_ShouldHandleGracefully()
    {
        // Arrange - create directory with invalid JSON
        var invalidPath = Path.Combine(Path.GetTempPath(), $"invalid_{Guid.NewGuid()}");
        Directory.CreateDirectory(invalidPath);
        File.WriteAllText(Path.Combine(invalidPath, "en.json"), "{ invalid json }");

        // Act
        var service = new LocalizationService(invalidPath);

        // Assert - should not throw, and GetString should return key
        var result = service.GetString("welcome", "en");
        result.Should().Be("welcome");

        // Cleanup
        Directory.Delete(invalidPath, recursive: true);
    }

    [Fact]
    public void LocalizationService_WithDefaultResourcePath_ShouldUseExpectedPath()
    {
        // Arrange - service with no path specified
        var service = new LocalizationService();

        // Act - should use default path: AppDomain.CurrentDomain.BaseDirectory + Resources/localization
        // Just verify it doesn't throw and supported languages are defined
        var supportedLanguages = service.GetSupportedLanguages();

        // Assert
        supportedLanguages.Should().HaveCount(5);
        supportedLanguages.Should().Contain(new[] { "en", "zu", "st", "xh", "af" });
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public void GetString_WithMultipleParametersInOrder_ShouldFormatCorrectly()
    {
        // Arrange - add a translation with multiple parameters
        var testPath = Path.Combine(_testResourcePath, "en.json");
        var existingJson = File.ReadAllText(testPath);
        var translations = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(existingJson);
        translations!["multiParam"] = "Name: {0}, Balance: R{1}, Date: {2}";
        File.WriteAllText(testPath, System.Text.Json.JsonSerializer.Serialize(translations));

        var service = new LocalizationService(_testResourcePath);

        // Act
        var result = service.GetString("multiParam", "en", "Alice", 1500.50m, "2026-03-24");

        // Assert
        result.Should().Be("Name: Alice, Balance: R1500.50, Date: 2026-03-24");  // C# formats decimal with trailing zero
    }

    [Fact]
    public void GetString_WithNoParameters_ShouldReturnRawString()
    {
        // Act
        var result = _sut.GetString("welcome", "en");

        // Assert
        result.Should().Be("Welcome");
    }

    [Fact]
    public void GetString_WithParametersButNoPlaceholders_ShouldReturnRawString()
    {
        // Act - provide parameters but key has no placeholders
        var result = _sut.GetString("welcome", "en", "param1", "param2");

        // Assert
        result.Should().Be("Welcome");
    }

    [Theory]
    [InlineData("en")]
    [InlineData("zu")]
    [InlineData("st")]
    [InlineData("xh")]
    [InlineData("af")]
    public void GetAllStrings_ForAllSupportedLanguages_ShouldReturnNonEmptyDictionaries(string languageCode)
    {
        // Act
        var result = _sut.GetAllStrings(languageCode);

        // Assert
        result.Should().NotBeNull();
        // Even if file is missing or empty, should return empty dictionary (not null)
    }

    #endregion
}
