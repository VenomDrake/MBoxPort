using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PostaBridge.Engine;
using Mail2Pst.Core.Parsing;
using PSTFileFormat;
using Xunit;

namespace PostaBridge.Tests;

public sealed class ConversionTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "PostaBridge-tests-" + Guid.NewGuid());
    public ConversionTests() => Directory.CreateDirectory(_root);
    public void Dispose() => Directory.Delete(_root, true);
    private string Source(string contents, string name = "Archivio.mbox")
    {
        string path = Path.Combine(_root, name);
        File.WriteAllText(path, contents, new UTF8Encoding(false));
        return path;
    }
    private static string Message(string subject = "Messaggio di prova", string body = "Ciao, questa è la posta.") =>
        $"From alice@example.org Tue Oct 20 10:00:00 2026\nFrom: Alice <alice@example.org>\nTo: Bob <bob@example.org>\nCc: Clara <clara@example.org>\nSubject: {subject}\nDate: Tue, 20 Oct 2026 10:00:00 +0200\nMessage-ID: <{Guid.NewGuid()}@example.org>\nMIME-Version: 1.0\nContent-Type: text/plain; charset=utf-8\n\n{body}\n\n";

    [Fact]
    public void UnicodeAndEscapedFromSurviveAndSourceIsUnchanged()
    {
        string source = Source(Message("Caffè e città", "Una riga\n>From un messaggio\nFine."));
        byte[] hash = SHA256.HashData(File.ReadAllBytes(source));
        var events = new List<JobProgress>();
        var result = new ConversionService().Convert(source, _root, 20480, events.Add);
        Assert.Equal(1, result.Report.ConvertedCount);
        Assert.Equal(0, result.Report.SkippedCount);
        Assert.Equal(hash, SHA256.HashData(File.ReadAllBytes(source)));
        var pst = new PSTFile(Assert.Single(result.Report.OutputFiles), FileAccess.Read);
        try
        {
            var folder = (MailFolder)pst.TopOfPersonalFolders.FindChildFolder("Posta importata")!;
            var note = folder.GetNote(0);
            Assert.Equal("Caffè e città", note.Subject);
            Assert.Contains("From un messaggio", note.Body);
            Assert.Equal(1, folder.MessageCount);
        }
        finally { pst.CloseFile(); }
        Assert.Equal("scan", events.First().Phase);
        Assert.Equal("done", events.Last().Phase);
        Assert.Equal(100, events.Last().Percent);
        Assert.True(File.Exists(Path.Combine(result.Directory, "rapporto.txt")));
    }

    [Fact]
    public void RepeatRunDoesNotOverwriteAnything()
    {
        string source = Source(Message());
        var service = new ConversionService();
        var first = service.Convert(source, _root, 20480, _ => { });
        string pst = Assert.Single(first.Report.OutputFiles);
        byte[] before = SHA256.HashData(File.ReadAllBytes(pst));
        var second = service.Convert(source, _root, 20480, _ => { });
        Assert.NotEqual(first.Directory, second.Directory);
        Assert.Equal(before, SHA256.HashData(File.ReadAllBytes(pst)));
    }

    [Fact]
    public void ScanCancellationCreatesNoPst()
    {
        string source = Source(Message(body: string.Concat(Enumerable.Repeat("Testo del messaggio.\n", 30000))));
        using var cts = new CancellationTokenSource();
        Assert.Throws<OperationCanceledException>(() => new ConversionService().Convert(source, _root, 20480,
            p => { if (p.Phase == "scan" && p.Bytes > 0) cts.Cancel(); }, cts.Token));
        Assert.Empty(Directory.GetFiles(_root, "*.pst", SearchOption.AllDirectories));
    }

    [Fact]
    public void ConversionCancellationReportsPartialState()
    {
        string source = Source(string.Concat(Enumerable.Range(0, 200).Select(i => Message($"Mail {i}"))));
        using var cts = new CancellationTokenSource();
        var result = new ConversionService().Convert(source, _root, 20480,
            p => { if (p.Phase == "convert" && p.Converted >= 10) cts.Cancel(); }, cts.Token);
        Assert.True(result.Report.Cancelled);
        Assert.Equal(0, result.Report.ConvertedCount);
        Assert.True(result.Report.DiscardedCount >= 10);
        Assert.True(File.Exists(Path.Combine(result.Directory, "rapporto.txt")));
        Assert.Empty(Directory.GetFiles(result.Directory, "*.pst"));
        using var session = JsonDocument.Parse(File.ReadAllText(Path.Combine(result.Directory, "sessione.json")));
        Assert.False(session.RootElement.GetProperty("completed").GetBoolean());
        Assert.Equal(0, session.RootElement.GetProperty("converted").GetInt32());
        Assert.Equal(result.Report.DiscardedCount, session.RootElement.GetProperty("discarded").GetInt32());
        Assert.Contains("Nessun file PST completo conservato", File.ReadAllText(Path.Combine(result.Directory, "rapporto.txt")));
    }

    [Fact]
    public void CancellationReportsOnlyMessagesInPreservedParts()
    {
        string body = string.Concat(Enumerable.Repeat(new string('x', 80) + "\n", 600));
        string source = Source(string.Concat(Enumerable.Range(0, 80).Select(i => Message($"Mail {i}", body))));
        using var cts = new CancellationTokenSource();
        var result = new ConversionService().Convert(source, _root, 1,
            p => { if (p.Phase == "convert" && p.Converted >= 40) cts.Cancel(); }, cts.Token);

        Assert.True(result.Report.Cancelled);
        Assert.NotEmpty(result.Report.OutputFiles);
        Assert.True(result.Report.DiscardedCount > 0);
        int saved = 0;
        foreach (string path in result.Report.OutputFiles)
        {
            var pst = new PSTFile(path, FileAccess.Read);
            try { saved += pst.TopOfPersonalFolders.GetChildFolders().Sum(folder => folder.MessageCount); }
            finally { pst.CloseFile(); }
        }
        Assert.True(saved > 0);
        Assert.Equal(saved, result.Report.ConvertedCount);
        Assert.Equal(saved + result.Report.DiscardedCount + result.Report.SkippedCount, result.Report.ProcessedCount);
        Assert.Equal(result.Report.OutputFiles.Count, Directory.GetFiles(result.Directory, "*.pst").Length);
        using var session = JsonDocument.Parse(File.ReadAllText(Path.Combine(result.Directory, "sessione.json")));
        Assert.Equal(saved, session.RootElement.GetProperty("converted").GetInt32());
        Assert.Equal(80 - result.Report.ProcessedCount, session.RootElement.GetProperty("unprocessed").GetInt32());
    }

    [Fact]
    public void FailedDiagnosticWritePreservesOriginalErrorAndResultsDirectory()
    {
        var originalError = new IOException("Errore sintetico dopo la conversione.");
        string? output = null;
        var failure = Assert.Throws<ConversionFailedException>(() =>
            new ConversionService().Convert(Source(Message()), _root, 20480, p =>
            {
                if (p.Phase != "done") return;
                output = Assert.Single(Directory.GetDirectories(_root));
                // A directory at the diagnostic filename makes File.WriteAllText
                // fail independently from the original conversion error.
                Directory.CreateDirectory(Path.Combine(output, "ERRORE.txt"));
                throw originalError;
            }));

        Assert.Same(originalError, failure.InnerException);
        Assert.Equal(output, failure.OutputDirectory);
        Assert.NotEmpty(Directory.GetFiles(failure.OutputDirectory, "*.pst"));
        using var session = JsonDocument.Parse(File.ReadAllText(Path.Combine(failure.OutputDirectory, "sessione.json")));
        Assert.True(session.RootElement.GetProperty("failed").GetBoolean());
        Assert.False(session.RootElement.GetProperty("completed").GetBoolean());
    }

    [Fact]
    public void SmallSplitLimitProducesMultipleReadablePsts()
    {
        string body = string.Concat(Enumerable.Repeat("Archivio di prova con una lunga riga di testo per verificare la divisione.\n", 600));
        string source = Source(string.Concat(Enumerable.Range(0, 60).Select(i => Message($"Mail {i}", body))));
        var result = new ConversionService().Convert(source, _root, 1, _ => { });
        Assert.True(result.Report.OutputFiles.Count > 1);
        Assert.Equal(60, result.Report.ConvertedCount);
        int total = 0;
        foreach (string file in result.Report.OutputFiles)
        {
            var pst = new PSTFile(file, FileAccess.Read);
            try { total += pst.TopOfPersonalFolders.GetChildFolders().Sum(f => f.MessageCount); }
            finally { pst.CloseFile(); }
        }
        Assert.Equal(60, total);
    }

    [Fact]
    public void EmptyInputIsRejectedWithoutOutput()
    {
        string source = Source("");
        Assert.Throws<InvalidDataException>(() => new ConversionService().Convert(source, _root, 20480, _ => { }));
        Assert.Empty(Directory.GetFiles(_root, "*.pst", SearchOption.AllDirectories));
    }

    [Fact]
    public void MissingDestinationIsRejected()
    {
        Assert.Throws<DirectoryNotFoundException>(() => new ConversionService().Convert(Source(Message()), Path.Combine(_root, "missing"), 20480, _ => { }));
    }

    [Theory]
    [InlineData("CON", "Archivio_CON")]
    [InlineData("../Archivio", "_Archivio")]
    [InlineData("  . ", "Archivio")]
    [InlineData("Caffè", "Caffè")]
    public void OutputNamesAreSafe(string input, string expected) => Assert.Equal(expected, ConversionService.SafeStem(input));

    [Fact]
    public void CountingWithProgressUsesTheSameBoundaries()
    {
        string source = Source(Message(body: "Prima\n>From corpo\nDopo") + Message());
        var parser = new MboxParser();
        long bytes = 0;
        Assert.Equal(parser.CountMessages(source), parser.CountMessagesWithProgress(source, b => bytes = b));
        Assert.Equal(new FileInfo(source).Length, bytes);
    }

    [Fact]
    public void RandomTextIsNotReportedAsConvertedMail()
    {
        Assert.Throws<InvalidDataException>(() => new ConversionService().Convert(Source("un documento di testo"), _root, 20480, _ => { }));
        Assert.Empty(Directory.GetFiles(_root, "*.pst", SearchOption.AllDirectories));
    }
}
