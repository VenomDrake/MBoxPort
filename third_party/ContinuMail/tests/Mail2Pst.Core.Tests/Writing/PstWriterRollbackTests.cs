// SPDX-License-Identifier: GPL-3.0-or-later
using Mail2Pst.Core.Mapping;
using Mail2Pst.Core.Models;
using Mail2Pst.Core.Progress;
using Mail2Pst.Core.Reporting;
using Mail2Pst.Core.Writing;
using Xunit;

namespace Mail2Pst.Core.Tests.Writing;

public class PstWriterRollbackTests
{
    [Fact]
    public void WritePlan_CancelDuringAppointments_DiscardsAllItemTypesInCurrentPart()
    {
        string dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var report = new ConversionReport();
            using var cts = new CancellationTokenSource();
            var messages = new[] { new PlannedMessage { TargetFolderPath = new[] { "Mail" },
                Message = new MailMessage { Subject = "Mail", TextBody = "Synthetic",
                    Source = new SourceReference { SourcePath = "source.mbox", Identifier = "1" } } } };
            var contacts = new[] { new PlannedContact { TargetFolderPath = new[] { "Contacts" },
                Contact = new ContactRecord { DisplayName = "Example", Emails = { "alice@example.org" } } } };
            var tasks = new[] { new PlannedTask { TargetFolderPath = new[] { "Tasks" },
                Task = new TaskRecord { Subject = "Task", SourceId = "task-1" } } };
            var appointments = new[] { new PlannedAppointment { TargetFolderPath = new[] { "Appointments" },
                Appointment = new AppointmentRecord { Subject = "Appointment", SourceId = "appointment-1",
                    StartUtc = new DateTime(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc),
                    EndUtc = new DateTime(2026, 11, 4, 13, 0, 0, DateTimeKind.Utc) } } };

            Assert.Throws<OperationCanceledException>(() => new PstWriter().WritePlan(
                new PstOutputPlan { Name = "Out", MaxSizeBytes = long.MaxValue }, messages,
                contacts, new IReadOnlyList<string>[] { new[] { "Contacts" } },
                tasks, new IReadOnlyList<string>[] { new[] { "Tasks" } },
                appointments, new IReadOnlyList<string>[] { new[] { "Appointments" } },
                dir, report, onProgress: p => { if (p is ProgressEvent { Phase: "appointments" }) cts.Cancel(); },
                cancellationToken: cts.Token));

            Assert.Equal(0, report.TotalWrittenItems);
            Assert.Equal(0, report.ConvertedCount);
            Assert.Equal(0, report.ContactsConverted);
            Assert.Equal(0, report.TasksConverted);
            Assert.Equal(0, report.AppointmentsConverted);
            Assert.Equal(1, report.DiscardedCount);
            Assert.Equal(1, report.ContactsDiscarded);
            Assert.Equal(1, report.TasksDiscarded);
            Assert.Equal(1, report.AppointmentsDiscarded);
            Assert.Equal(4, report.TotalDiscardedItems);
            Assert.Equal(1, report.ProcessedCount);
            Assert.Empty(report.OutputFiles);
            Assert.Single(report.DeletedFiles);
            Assert.Empty(Directory.GetFiles(dir, "*.pst"));
        }
        finally { Directory.Delete(dir, true); }
    }
}
