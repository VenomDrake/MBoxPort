// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;

namespace PostaBridge.Engine.Localization;

public sealed record LanguageOption(string Code, string NativeName, string CultureName, bool IsRightToLeft = false);

/// <summary>An immutable language and catalog. Formatting never changes the process or thread culture.</summary>
public sealed class AppText
{
    private const string MessageDataKey = "MboxPort.LocalizedMessage";
    private sealed record MessageSpec(string Key, object?[] Arguments);
    private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> Catalogs = new();
    public static IReadOnlyList<LanguageOption> SupportedLanguages { get; } = Array.AsReadOnly(new[]
    {
        new LanguageOption("en", "English", "en-US"),
        new LanguageOption("it", "Italiano", "it-IT"),
        new LanguageOption("zh-Hans", "简体中文", "zh-CN"),
        new LanguageOption("hi", "हिन्दी", "hi-IN"),
        new LanguageOption("es", "Español", "es-ES"),
        new LanguageOption("ar", "العربية", "ar-SA", true),
        new LanguageOption("fr", "Français", "fr-FR"),
        new LanguageOption("bn", "বাংলা", "bn-BD"),
        new LanguageOption("pt", "Português", "pt-BR"),
        new LanguageOption("id", "Bahasa Indonesia", "id-ID"),
    });

    private readonly IReadOnlyDictionary<string, string> _catalog;
    public string LanguageCode { get; }
    public CultureInfo Culture { get; }
    public bool IsRightToLeft { get; }
    public IReadOnlyDictionary<string, string> Entries => _catalog;

    public AppText(string? language)
    {
        LanguageCode = ResolveLanguage(language);
        var option = SupportedLanguages.Single(x => x.Code == LanguageCode);
        Culture = CultureInfo.GetCultureInfo(option.CultureName);
        IsRightToLeft = option.IsRightToLeft;
        _catalog = Catalogs.GetOrAdd(LanguageCode, ReadCatalog);
    }

    public static string ResolveLanguage(string? language)
    {
        var exact = SupportedLanguages.FirstOrDefault(x => x.Code.Equals(language, StringComparison.OrdinalIgnoreCase));
        if (exact is not null) return exact.Code;
        try
        {
            string iso = CultureInfo.GetCultureInfo(language ?? "").TwoLetterISOLanguageName;
            return SupportedLanguages.FirstOrDefault(x => x.Code == iso || iso == "zh" && x.Code == "zh-Hans")?.Code ?? "en";
        }
        catch (CultureNotFoundException) { return "en"; }
    }

    public string this[string key] => _catalog.TryGetValue(key, out string? value) ? value
        : Catalogs.GetOrAdd("en", ReadCatalog).TryGetValue(key, out string? fallback) ? fallback
        : throw new KeyNotFoundException($"Missing localization key: {key}");

    public string Format(string key, params object?[] arguments) => string.Format(Culture, this[key], arguments);

    // Keep the exception's original type, inner exception, and message for diagnostics while
    // retaining a language-independent description that can be rendered after a UI language change.
    public T Error<T>(T exception, string key, params object?[] arguments) where T : Exception
    {
        exception.Data[MessageDataKey] = new MessageSpec(key, (object?[])arguments.Clone());
        return exception;
    }

    public string DescribeError(Exception exception) => exception.Data[MessageDataKey] is MessageSpec spec
        ? Render(spec) : Format("error.diagnostic", exception.Message);

    private string Render(MessageSpec spec) => Format(spec.Key, spec.Arguments.Select(value => value is Exception ex
        ? ex.Data[MessageDataKey] is MessageSpec inner ? Render(inner) : ex.Message : value).ToArray());

    private static IReadOnlyDictionary<string, string> ReadCatalog(string code)
    {
        var assembly = typeof(AppText).Assembly;
        string resource = assembly.GetManifestResourceNames().Single(name => name.EndsWith($".Localization.{code}.json", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resource)!;
        var dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? throw new InvalidDataException($"Empty language catalog: {code}");
        return new ReadOnlyDictionary<string, string>(dictionary);
    }
}
