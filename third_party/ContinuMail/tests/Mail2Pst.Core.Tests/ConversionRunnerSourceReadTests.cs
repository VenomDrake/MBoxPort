// SPDX-License-Identifier: GPL-3.0-or-later
using Mail2Pst.Core.Config;
using Mail2Pst.Core.Models;
using Mail2Pst.Core.Parsing;
using Mail2Pst.Core.Writing;
using PSTFileFormat;
using Xunit;

namespace Mail2Pst.Core.Tests;

public class ConversionRunnerSourceReadTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Run_ReadFailureAfterOneMessage_IsFatalAndDiscardsIncompletePart(bool accessDenied)
    {
        string dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            string path = Path.Combine(dir, "source.mbox");
            Exception cause = accessDenied ? new UnauthorizedAccessException("denied") : new IOException("read failed");
            var parser = new FailingParser(1, cause);
            var runner = new ConversionRunner(new PstWriter(), _ => parser);
            var failure = Assert.Throws<SourceReadException>(() => runner.Run(Config(path), dir, _ => { }, precomputedTotalMessages: 3));

            Assert.Equal(path, failure.SourcePath);
            Assert.Same(cause, failure.InnerException);
            Assert.NotNull(failure.Report);
            Assert.Equal(0, failure.Report.ConvertedCount);
            Assert.Equal(1, failure.Report.DiscardedCount);
            Assert.Equal(1, failure.Report.ProcessedCount);
            Assert.False(failure.Report.Cancelled);
            Assert.Empty(failure.Report.Skipped);
            Assert.Empty(failure.Report.OutputFiles);
            Assert.Empty(Directory.GetFiles(dir, "*.pst"));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void Run_ReadFailureAfterSplitting_RetainsOnlyCompletedPartsAndTheirCounts()
    {
        string dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            string path = Path.Combine(dir, "source.mbox");
            var parser = new FailingParser(20, new IOException("read failed"), new string('x', 50000));
            var runner = new ConversionRunner(new PstWriter(), _ => parser);
            var config = Config(path);
            config.Outputs[0].MaxSizeMB = 1;
            var failure = Assert.Throws<SourceReadException>(() => runner.Run(config, dir));

            Assert.NotNull(failure.Report);
            var report = failure.Report;
            Assert.NotEmpty(report.OutputFiles);
            int retained = 0;
            foreach (string file in report.OutputFiles)
            {
                var pst = new PSTFile(file, FileAccess.Read);
                try { retained += Count(pst.TopOfPersonalFolders); }
                finally { pst.CloseFile(); }
            }
            Assert.Equal(retained, report.ConvertedCount);
            Assert.Equal(20, report.ConvertedCount + report.DiscardedCount);
            Assert.True(report.DiscardedCount > 0);
            Assert.Equal(report.OutputFiles.OrderBy(p => p), Directory.GetFiles(dir, "*.pst").OrderBy(p => p));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void Run_CountReadFailure_FailsBeforeCreatingAnyPst()
    {
        string dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var parser = new FailingParser(0, new IOException("count failed"), failCount: true);
            var runner = new ConversionRunner(new PstWriter(), _ => parser);
            var failure = Assert.Throws<SourceReadException>(() => runner.Run(Config(Path.Combine(dir, "source.mbox")), dir, _ => { }));
            Assert.Null(failure.Report);
            Assert.Empty(Directory.GetFiles(dir, "*.pst"));
        }
        finally { Directory.Delete(dir, true); }
    }

    private static ConversionConfig Config(string path) => new()
    {
        Outputs = { new OutputGroupConfig { Name = "Out", MaxSizeMB = 100,
            Sources = { new SourceConfig { Type = "mbox", Path = path, TargetFolder = "Imported" } } } }
    };

    private static int Count(PSTFolder folder) => folder.MessageCount + folder.GetChildFolders().Sum(Count);

    private sealed class FailingParser(int count, Exception failure, string body = "synthetic body", bool failCount = false) : IMailSourceParser
    {
        public int CountMessages(string path) => failCount ? throw failure : count + 1;

        public IEnumerable<ParseResult> Parse(string path, Action<long>? onBytesRead = null)
        {
            for (int i = 0; i < count; i++)
                yield return ParseResult.Ok(new MailMessage { Subject = $"Synthetic {i}", TextBody = body,
                    Source = new SourceReference { SourcePath = path, Identifier = $"message #{i + 1}" } });
            throw failure;
        }
    }
}
