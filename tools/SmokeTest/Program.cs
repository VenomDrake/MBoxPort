// Generates synthetic mail only. No real correspondence is used by this test.
using System.Text;
using System.Text.Json;
using PostaBridge.Engine;

string root = Path.GetFullPath(args.Length > 0 ? args[0] : "artifacts/smoke");
Directory.CreateDirectory(root);
var payload = Enumerable.Range(0, 65536).Select(i => (byte)(i % 251)).ToArray();
string base64 = Convert.ToBase64String(payload, Base64FormattingOptions.InsertLineBreaks);
string source = Path.Combine(root, "Archivio-esempio.mbox");
using (var writer = new StreamWriter(source, false, new UTF8Encoding(false)))
{
    for (int i = 0; i < 240; i++)
    {
        writer.Write($"From alice@example.org Tue Oct 20 10:00:00 2026\r\nFrom: Alice Esempio <alice@example.org>\r\nTo: Bob <bob@example.org>\r\nCc: Clara <clara@example.org>\r\nBcc: Davide <davide@example.org>\r\nSubject: =?UTF-8?B?{Convert.ToBase64String(Encoding.UTF8.GetBytes($"Caffè e città — {i}"))}?=\r\nDate: Tue, 20 Oct 2026 10:00:00 +0200\r\nMessage-ID: <smoke-{i}@example.org>\r\nMIME-Version: 1.0\r\nContent-Type: multipart/mixed; boundary=\"outer\"\r\n\r\n--outer\r\nContent-Type: multipart/alternative; boundary=\"inner\"\r\n\r\n--inner\r\nContent-Type: text/plain; charset=utf-8\r\n\r\nCiao, città e caffè.\r\n>From una riga nel corpo\r\n\r\n--inner\r\nContent-Type: text/html; charset=utf-8\r\n\r\n<html><body><b>Ciao, città e caffè.</b></body></html>\r\n--inner--\r\n--outer\r\nContent-Type: application/octet-stream; name=\"prova.bin\"\r\nContent-Disposition: attachment; filename=\"prova.bin\"\r\nContent-Transfer-Encoding: base64\r\n\r\n{base64}\r\n--outer--\r\n\r\n");
    }
}
File.WriteAllBytes(Path.Combine(root, "expected-attachment.bin"), payload);
var result = new ConversionService().Convert(source, root, 2, p =>
{
    if (p.Phase is "done") Console.WriteLine($"{p.Converted}/{p.Total} messaggi convertiti");
});
if (result.Report.ConvertedCount != 240 || result.Report.SkippedCount != 0 || result.Report.OutputFiles.Count < 2)
    throw new Exception("Smoke test failed: conversion count, skips or splitting.");
File.WriteAllText(Path.Combine(root, "outputs.json"), JsonSerializer.Serialize(result.Report.OutputFiles));
Console.WriteLine(result.Directory);
