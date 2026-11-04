using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PostaBridge.Engine;
using PostaBridge.Engine.Localization;
using PSTFileFormat;
using Xunit;

namespace PostaBridge.Tests;

public sealed class LocalizationTests
{
    [Fact]
    public void AllTenCatalogs_AreCompleteAndHaveCompatibleCompositeFormats()
    {
        var baseline = new AppText("en");
        Assert.Equal(10, AppText.SupportedLanguages.Count);
        foreach (var language in AppText.SupportedLanguages)
        {
            var text = new AppText(language.Code);
            Assert.Equal(baseline.Entries.Keys.Order(), text.Entries.Keys.Order());
            foreach (var entry in baseline.Entries)
            {
                string translated = text.Entries[entry.Key];
                Assert.False(string.IsNullOrWhiteSpace(translated), $"{language.Code}/{entry.Key}");
                Assert.Equal(Placeholders(entry.Value), Placeholders(translated));
                var format = CompositeFormat.Parse(translated);
                object?[] values = Enumerable.Repeat<object?>(12345, format.MinimumArgumentCount).ToArray();
                Assert.False(string.IsNullOrWhiteSpace(text.Format(entry.Key, values)));
            }
            string filter = text["picker.sourceFilter"];
            Assert.Equal(4, filter.Split('|').Length);
            Assert.Equal("*.mbox;*.mbx", filter.Split('|')[1]);
            Assert.Equal("*.*", filter.Split('|')[3]);
        }
    }

    [Theory]
    [InlineData("it-CH", "it")]
    [InlineData("en-AU", "en")]
    [InlineData("zh-SG", "zh-Hans")]
    [InlineData("pt-PT", "pt")]
    [InlineData("ar-EG", "ar")]
    [InlineData("de-DE", "en")]
    [InlineData("not_a_culture", "en")]
    public void SystemLanguage_ResolvesRegionalVariantsAndFallsBackToEnglish(string requested, string expected)
        => Assert.Equal(expected, AppText.ResolveLanguage(requested));

    [Fact]
    public void LanguageInstances_UseTheirOwnCultureAndPreserveTheCallersCulture()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        var english = new AppText("en");
        var italian = new AppText("it");
        Assert.Contains("12,345", english.Format("report.detected", 12345));
        Assert.Contains("12.345", italian.Format("report.detected", 12345));
        Assert.Same(original, CultureInfo.CurrentCulture);
        Assert.True(new AppText("ar").IsRightToLeft);
        Assert.All(AppText.SupportedLanguages.Where(x => x.Code != "ar"), x => Assert.False(new AppText(x.Code).IsRightToLeft));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("\"it\"")]
    [InlineData("{broken")]
    [InlineData("{\"language\":42}")]
    public void InvalidPreferences_FallBackToTheSystemLanguage(string contents)
    {
        string dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            string path = Path.Combine(dir, "settings.json");
            File.WriteAllText(path, contents);
            Assert.Equal("fr", LanguagePreferences.Load(path, "fr-FR"));
            LanguagePreferences.Save("ar", path);
            Assert.Equal("ar", LanguagePreferences.Load(path, "it-IT"));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void KnownValidationError_CanBeRenderedInAnotherLanguageWithoutLosingItsType()
    {
        string dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            string source = Path.Combine(dir, "empty.mbox");
            File.WriteAllText(source, "");
            var error = Assert.Throws<InvalidDataException>(() => new ConversionService("en").Convert(source, dir, 20480, _ => { }));
            Assert.Equal(new AppText("en")["error.emptyMbox"], error.Message);
            Assert.Equal(new AppText("it")["error.emptyMbox"], new AppText("it").DescribeError(error));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Theory]
    [InlineData("en")]
    [InlineData("ar")]
    public void Conversion_UsesSelectedLanguageForPstFolderAndHumanReport(string language)
    {
        string dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            string source = Path.Combine(dir, "source.mbox");
            File.WriteAllText(source, "From alice@example.org Wed Nov 4 12:00:00 2026\nFrom: Alice <alice@example.org>\nTo: Bob <bob@example.org>\nSubject: Synthetic mail\nDate: Wed, 4 Nov 2026 12:00:00 +0100\nContent-Type: text/plain; charset=utf-8\n\nSynthetic body\n\n", new UTF8Encoding(false));
            var result = new ConversionService(language).Convert(source, dir, 20480, _ => { });
            var text = new AppText(language);
            var pst = new PSTFile(Assert.Single(result.Report.OutputFiles), FileAccess.Read);
            try
            {
                var folder = Assert.IsType<MailFolder>(pst.TopOfPersonalFolders.FindChildFolder(text["folder.imported"]));
                Assert.Equal(1, folder.MessageCount);
            }
            finally { pst.CloseFile(); }
            string report = File.ReadAllText(Path.Combine(result.Directory, "rapporto.txt"));
            Assert.Contains(text.Format("report.saved", 1), report);
            Assert.Contains(text["report.diagnostics"], report);
            using var session = JsonDocument.Parse(File.ReadAllText(Path.Combine(result.Directory, "sessione.json")));
            Assert.Equal(language, session.RootElement.GetProperty("language").GetString());
            Assert.Equal(ProductInfo.Name, session.RootElement.GetProperty("application").GetString());
            Assert.True(session.RootElement.GetProperty("completed").GetBoolean());
        }
        finally { Directory.Delete(dir, true); }
    }

    private static string[] Placeholders(string text) => Regex.Matches(text, @"(?<!\{)\{\d+(?:,[^}:]+)?(?::[^}]+)?\}")
        .Select(match => match.Value).Order().ToArray();
}
