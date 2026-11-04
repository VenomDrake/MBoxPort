// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text.Json;
using Mail2Pst.Core.Models;
using Mail2Pst.Core.Reporting;
using Xunit;

namespace Mail2Pst.Core.Tests.Reporting;

public class ConversionReportRollbackTests
{
    [Fact]
    public void DiscardCurrentPart_KeepsPreviousCountsAndSerializesPartialResults()
    {
        var report = new ConversionReport();
        report.RecordConverted();
        report.RecordConverted();
        report.RecordContactConverted();
        report.RecordContactConverted();
        report.RecordTaskConverted();
        report.RecordTaskConverted();
        report.RecordAppointmentConverted();
        report.RecordAppointmentConverted();
        report.RecordSkipped(new SourceReference { SourcePath = "source.mbox", Identifier = "3" }, "bad MIME");
        report.DiscardWrittenItems(1, 1, 1, 1);
        report.AddOutputFiles(new[] { "Out-1.pst" });
        report.RecordDeletedFile("Out-2.pst");
        report.MarkCancelled();

        Assert.Equal(4, report.TotalWrittenItems);
        Assert.Equal(4, report.TotalDiscardedItems);
        Assert.Equal(3, report.ProcessedCount);
        using var json = JsonDocument.Parse(report.ToJson());
        var root = json.RootElement;
        Assert.Equal(1, root.GetProperty("converted").GetInt32());
        Assert.Equal(1, root.GetProperty("discarded").GetInt32());
        Assert.Equal(3, root.GetProperty("processed").GetInt32());
        Assert.True(root.GetProperty("cancelled").GetBoolean());
        Assert.Equal("Out-1.pst", root.GetProperty("outputFiles")[0].GetString());
        Assert.Equal("Out-2.pst", root.GetProperty("deletedFiles")[0].GetString());
        foreach (string type in new[] { "contacts", "tasks", "appointments" })
        {
            Assert.Equal(1, root.GetProperty(type + "Converted").GetInt32());
            Assert.Equal(1, root.GetProperty(type + "Discarded").GetInt32());
        }
        string summary = report.ToSummary();
        Assert.Contains("Converted: 1", summary);
        Assert.Contains("Processed: 3", summary);
        Assert.Contains("Discarded from incomplete parts: 1", summary);
        Assert.Contains("Cancelled: True", summary);
    }
}
