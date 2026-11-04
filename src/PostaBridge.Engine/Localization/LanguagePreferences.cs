// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;
using System.Text.Json;

namespace PostaBridge.Engine.Localization;

public static class LanguagePreferences
{
    private static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ProductInfo.Name, "settings.json");

    public static string Load(string? path = null, string? systemLanguage = null)
    {
        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(path ?? DefaultPath));
            if (json.RootElement.ValueKind == JsonValueKind.Object && json.RootElement.TryGetProperty("language", out var language) && language.ValueKind == JsonValueKind.String)
                return AppText.ResolveLanguage(language.GetString());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { }
        return AppText.ResolveLanguage(systemLanguage ?? CultureInfo.CurrentUICulture.Name);
    }

    public static void Save(string language, string? path = null)
    {
        string target = path ?? DefaultPath;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(target))!);
            File.WriteAllText(target, JsonSerializer.Serialize(new { language = AppText.ResolveLanguage(language) }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}
