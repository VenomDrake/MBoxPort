// SPDX-License-Identifier: GPL-3.0-or-later
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Mail2Pst.Core;
using Mail2Pst.Core.Config;
using Mail2Pst.Core.Parsing;
using Mail2Pst.Core.Progress;
using Mail2Pst.Core.Reporting;
using PostaBridge.Engine.Localization;

namespace PostaBridge.Engine;

public record JobProgress(string Phase, double? Percent, int Converted = 0, int Total = 0,
    int Skipped = 0, int Warnings = 0, long Bytes = 0, double Seconds = 0);
public record JobResult(string Directory, ConversionReport Report, int Total, double Seconds);

public sealed class ConversionService
{
    private readonly AppText _text;
    public ConversionService(string language = "it") => _text = new AppText(language);

    public JobResult Convert(string sourcePath, string destination, long splitSizeMb,
        Action<JobProgress> onProgress, CancellationToken cancellationToken = default)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        sourcePath = Path.GetFullPath(sourcePath);
        destination = Path.GetFullPath(destination);
        if (splitSizeMb is < 1 or > 51200) throw _text.Error(new ArgumentException(_text["error.invalidSplit"]), "error.invalidSplit");
        if (!Directory.Exists(destination)) throw _text.Error(new DirectoryNotFoundException(_text["error.destinationMissing"]), "error.destinationMissing");
        // Hold a read-only lease across both passes: another process cannot modify or
        // delete the archive between counting and conversion on Windows.
        using var lease = OpenSource(sourcePath);
        if (lease.Length == 0) throw _text.Error(new InvalidDataException(_text["error.emptyMbox"]), "error.emptyMbox");
        if (lease.ReadByte() == '!' && lease.ReadByte() == 'B' && lease.ReadByte() == 'D' && lease.ReadByte() == 'N')
            throw _text.Error(new InvalidDataException(_text["error.alreadyPst"]), "error.alreadyPst");
        lease.Position = 0;
        byte[] prefixBytes = new byte[(int)Math.Min(128 * 1024, lease.Length)];
        int prefixLength = lease.Read(prefixBytes);
        string prefix = Encoding.ASCII.GetString(prefixBytes, 0, prefixLength);
        if (!prefix.StartsWith("From ", StringComparison.Ordinal) ||
            !Regex.IsMatch(prefix, @"(?m)^(From|To|Subject|Date|Message-ID|MIME-Version):", RegexOptions.IgnoreCase))
            throw _text.Error(new InvalidDataException(_text["error.invalidMbox"]), "error.invalidMbox");

        string stem = SafeStem(Path.GetFileNameWithoutExtension(sourcePath));
        var stopwatch = Stopwatch.StartNew();
        onProgress(new("scan", 0));
        long lastUpdate = 0;
        int total = new MboxParser().CountMessagesWithProgress(sourcePath, bytes =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (stopwatch.ElapsedMilliseconds - lastUpdate < 100 && bytes < lease.Length) return;
            lastUpdate = stopwatch.ElapsedMilliseconds;
            onProgress(new("scan", Math.Min(100d, bytes * 100d / lease.Length), Bytes: bytes, Seconds: stopwatch.Elapsed.TotalSeconds));
        }, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (total == 0) throw _text.Error(new InvalidDataException(_text["error.noMessages"]), "error.noMessages");

        // Each run owns a unique directory. Existing PSTs and reports are never reused.
        string suffix = Guid.NewGuid().ToString("N")[..8];
        string runDirectory = Path.Combine(destination, $"{stem}_{DateTime.Now:yyyyMMdd_HHmmss}_{suffix}");
        // Use an atomic create-new marker to reserve the output directory, even if two
        // application instances start at the same time.
        Directory.CreateDirectory(runDirectory);
        using (new FileStream(Path.Combine(runDirectory, ".postabridge"), FileMode.CreateNew)) { }

        var config = new ConversionConfig
        {
            Outputs = { new OutputGroupConfig
            {
                Name = stem, MaxSizeMB = splitSizeMb, FolderMapping = FolderMappingMode.Mirror,
                Sources = { new SourceConfig { Path = sourcePath, Type = "mbox", TargetFolder = _text["folder.imported"] } }
            } }
        };
        onProgress(new("convert", 0, Total: total, Seconds: stopwatch.Elapsed.TotalSeconds));
        ConversionReport? report = null;
        try
        {
            report = new ConversionRunner(checkIntervalMessages: 100, progressIntervalMessages: 10).Run(
                config, runDirectory, evt =>
                {
                    switch (evt)
                    {
                        case ProgressEvent p:
                            double percent = Math.Min(99.9, (p.Converted + p.Skipped) * 100d / total);
                            onProgress(new("convert", percent, p.Converted, total, p.Skipped, p.Warnings,
                                p.EstimatedOutputBytes, stopwatch.Elapsed.TotalSeconds));
                            break;
                        case WarningEvent:
                            break; // The complete warning text is retained in the conversion report.
                    }
                }, cancellationToken, precomputedTotalMessages: total);
            stopwatch.Stop();
            // This application imports one MBOX without intentional exclusions. Every
            // scanned message must have a final outcome before completion is announced.
            if (!report.Cancelled && (long)report.ConvertedCount + report.SkippedCount != total)
                throw _text.Error(new InvalidDataException(_text.Format("error.incomplete", total, report.ConvertedCount, report.SkippedCount)),
                    "error.incomplete", total, report.ConvertedCount, report.SkippedCount);
            WriteReports(runDirectory, sourcePath, report, total, stopwatch.Elapsed.TotalSeconds);
            onProgress(new(report.Cancelled ? "cancelled" : "done", report.Cancelled ? null : 100,
                report.ConvertedCount, total, report.SkippedCount, report.WarningCount,
                Seconds: stopwatch.Elapsed.TotalSeconds));
            return new(runDirectory, report, total, stopwatch.Elapsed.TotalSeconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var partialReport = report ?? (ex as SourceReadException)?.Report;
            if (partialReport is not null)
            {
                try { WriteReports(runDirectory, sourcePath, partialReport, total, stopwatch.Elapsed.TotalSeconds, ex.Message); }
                catch (Exception diagnosticError) when (diagnosticError is IOException or UnauthorizedAccessException)
                {
                    // A full or disconnected output disk must not hide the original
                    // error or the location of any already completed PST parts.
                }
            }
            try
            {
                File.WriteAllText(Path.Combine(runDirectory, "ERRORE.txt"),
                    $"{_text["error.fileIntro"]}\n\n{_text.Format("error.diagnostic", ex)}\n", Encoding.UTF8);
            }
            catch (Exception diagnosticError) when (diagnosticError is IOException or UnauthorizedAccessException) { }
            string key = ex is SourceReadException ? "error.sourceRead" : "error.interrupted";
            throw _text.Error(new ConversionFailedException(_text.Format(key, ex.Message), runDirectory, ex), key, ex);
        }
    }

    private FileStream OpenSource(string path)
    {
        try { return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read); }
        catch (FileNotFoundException ex) { throw _text.Error(new FileNotFoundException(_text["error.sourceMissing"], path, ex), "error.sourceMissing"); }
        catch (DirectoryNotFoundException ex) { throw _text.Error(new DirectoryNotFoundException(_text["error.sourceMissing"], ex), "error.sourceMissing"); }
    }

    public static string SafeStem(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().Concat("<>:\"/\\|?*").ToHashSet();
        string stem = new(value.Select(c => invalid.Contains(c) || char.IsControl(c) ? '_' : c).ToArray());
        stem = stem.Trim(' ', '.');
        if (stem.Length > 80) stem = stem[..80].TrimEnd(' ', '.');
        if (string.IsNullOrWhiteSpace(stem) || stem is "." or "..") stem = "Archivio";
        string first = stem.Split('.')[0].ToUpperInvariant();
        if (new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" }.Contains(first))
            stem = "Archivio_" + stem;
        return stem;
    }

    private void WriteReports(string directory, string source, ConversionReport report, int total, double seconds,
        string? error = null)
    {
        File.WriteAllText(Path.Combine(directory, "rapporto-dettagliato.json"), report.ToJson(), Encoding.UTF8);
        string status = _text[error is not null ? "report.statusFailed" : report.Cancelled ? "report.statusCancelled"
            : report.SkippedCount > 0 || report.WarningCount > 0 ? "report.statusNotices" : "report.statusComplete"];
        var text = new StringBuilder().AppendLine(_text.Format("report.title", ProductInfo.Name, ProductInfo.Version, status)).AppendLine()
            .AppendLine(_text.Format("report.source", source))
            .AppendLine(_text.Format("report.detected", total))
            .AppendLine(_text.Format("report.processed", report.ProcessedCount))
            .AppendLine(_text.Format("report.saved", report.ConvertedCount))
            .AppendLine(_text.Format("report.discarded", report.DiscardedCount))
            .AppendLine(_text.Format("report.skipped", report.SkippedCount))
            .AppendLine(_text.Format("report.unprocessed", Math.Max(0, total - report.ProcessedCount)))
            .AppendLine(_text.Format("report.warnings", report.WarningCount))
            .AppendLine(_text.Format("report.duration", TimeSpan.FromSeconds(seconds).ToString("g", _text.Culture)))
            .AppendLine().AppendLine(_text["report.files"]);
        foreach (string file in report.OutputFiles) text.AppendLine(file);
        if (report.OutputFiles.Count == 0) text.AppendLine(_text["report.noFiles"]);
        if (report.Cancelled) text.AppendLine().AppendLine(_text["report.cancelledNote"]);
        if (error is not null) text.AppendLine().AppendLine(_text.Format("report.error", error)).AppendLine(_text["report.failedNote"]);
        text.AppendLine().AppendLine(_text["report.diagnostics"]).Append(report.ToSummary());
        File.WriteAllText(Path.Combine(directory, "rapporto.txt"), text.ToString(), Encoding.UTF8);
        File.WriteAllText(Path.Combine(directory, "sessione.json"), JsonSerializer.Serialize(new
        {
            application = ProductInfo.Name, version = ProductInfo.Version, language = _text.LanguageCode, source, total,
            completed = error is null && !report.Cancelled, failed = error is not null, error,
            cancelled = report.Cancelled, processed = report.ProcessedCount,
            converted = report.ConvertedCount, discarded = report.DiscardedCount,
            unprocessed = Math.Max(0, total - report.ProcessedCount), skipped = report.SkippedCount,
            warnings = report.WarningCount, seconds, outputs = report.OutputFiles
        }, new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
    }
}

public sealed class ConversionFailedException(string message, string outputDirectory, Exception inner) : Exception(message, inner)
{
    public string OutputDirectory { get; } = outputDirectory;
}
