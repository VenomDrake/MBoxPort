// SPDX-License-Identifier: GPL-3.0-or-later
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Markup;
using Microsoft.Win32;
using PostaBridge.Engine;
using PostaBridge.Engine.Localization;

namespace PostaBridge;

public partial class MainWindow : Window
{
    private enum ViewState { Idle, Ready, Scan, Convert, Done, Cancelled, ScanCancelled, Failed, Cancelling }
    private AppText _text = new(LanguagePreferences.Load());
    private bool _initialized;
    private string? _source;
    private string? _output;
    private CancellationTokenSource? _cancellation;
    private bool _busy;
    private bool _hasRun;
    private bool _closeRequested;
    private ViewState _state;
    private JobProgress? _lastProgress;
    private JobResult? _lastResult;
    private Exception? _errorException;
    private string? _errorKey;
    private object?[] _errorArguments = [];
    private readonly Stopwatch _clock = new();
    private readonly System.Windows.Threading.DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    public MainWindow()
    {
        InitializeComponent();
        LanguageBox.ItemsSource = AppText.SupportedLanguages;
        LanguageBox.SelectedValue = _text.LanguageCode;
        DestinationBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        _timer.Tick += (_, _) => RenderStats();
        _initialized = true;
        ApplyLanguage();
    }

    private void LanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized || _busy || LanguageBox.SelectedValue is not string language) return;
        _text = new AppText(language);
        LanguagePreferences.Save(_text.LanguageCode);
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        Language = XmlLanguage.GetLanguage(_text.Culture.IetfLanguageTag);
        FlowDirection = _text.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        Title = _text.Format("app.windowTitle", ProductInfo.Name);
        ProductNameLabel.Text = ProductInfo.Name;
        TaglineLabel.Text = _text["ui.tagline"];
        HeadlineLabel.Text = _text["ui.headline"];
        DescriptionLabel.Text = _text["ui.description"];
        SourceHeading.Text = _text["ui.source"];
        DestinationHeading.Text = _text["ui.destination"];
        DestinationNote.Text = _text["ui.destinationNote"];
        DestinationBox.ToolTip = _text["ui.destinationTip"];
        SplitLabel.Text = _text["ui.split"];
        SplitNote.Text = _text["ui.splitNote"];
        PrivacyLabel.Text = _text["ui.privacy"];
        AboutButton.Content = _text["ui.about"];
        LanguageLabel.Text = _text["ui.language"];
        DestinationButton.Content = _text["button.browse"];
        OpenButton.Content = _text["button.results"];
        ReportButton.Content = _text["button.report"];
        foreach (ComboBoxItem option in SplitBox.Items)
            option.Content = _text.Format("ui.splitOption", long.Parse(option.Tag.ToString()!, CultureInfo.InvariantCulture) / 1024);
        AutomationProperties.SetName(SourceButton, _text["a11y.source"]);
        AutomationProperties.SetName(DestinationBox, _text["ui.destination"]);
        AutomationProperties.SetName(DestinationButton, _text["picker.destinationTitle"]);
        AutomationProperties.SetName(SplitBox, _text["a11y.split"]);
        AutomationProperties.SetName(JobBar, _text["a11y.progress"]);
        AutomationProperties.SetName(StartButton, _text["button.start"]);
        AutomationProperties.SetName(LanguageBox, _text["ui.language"]);
        RenderSource();
        RenderState();
        RenderError();
    }

    private void ChooseSource(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog { Title = _text["picker.sourceTitle"], Filter = _text["picker.sourceFilter"],
            Multiselect = false, CheckFileExists = true };
        if (picker.ShowDialog(this) == true) SetSource(picker.FileName);
    }

    private void SetSource(string path)
    {
        if (_busy) return;
        try
        {
            var file = new FileInfo(path);
            if (!file.Exists) throw _text.Error(new FileNotFoundException(_text["error.sourceMissing"]), "error.sourceMissing");
            _source = file.FullName;
            _state = ViewState.Ready;
            _hasRun = false;
            _lastProgress = null;
            _lastResult = null;
            _output = null;
            OpenButton.Visibility = ReportButton.Visibility = Visibility.Collapsed;
            ClearError();
            RenderSource();
            RenderState();
            UpdateStart();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void RenderSource()
    {
        SourceButton.Content = _text[_source is null ? "button.choose" : "button.change"];
        SourceName.FlowDirection = SourceDetail.FlowDirection = _source is null
            ? FlowDirection : FlowDirection.LeftToRight;
        if (_source is null)
        {
            SourceName.Text = _text["ui.sourcePlaceholder"];
            SourceDetail.Text = _text["ui.sourceDrop"];
            return;
        }
        var file = new FileInfo(_source);
        SourceName.Text = file.Name;
        SourceDetail.Text = _text.Format("ui.sourceDetails", file.Exists ? FormatBytes(file.Length) : "—", file.DirectoryName);
        SourceCard.ToolTip = file.FullName;
    }

    private void ChooseDestination(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFolderDialog { Title = _text["picker.destinationTitle"], Multiselect = false };
        if (Directory.Exists(DestinationBox.Text)) picker.InitialDirectory = DestinationBox.Text;
        if (picker.ShowDialog(this) == true) DestinationBox.Text = picker.FolderName;
    }

    private void DestinationChanged(object sender, TextChangedEventArgs e) => UpdateStart();
    private void UpdateStart()
    {
        if (StartButton is not null) StartButton.IsEnabled = !_busy && _source is not null && Directory.Exists(DestinationBox.Text.Trim());
    }

    private async void StartConversion(object sender, RoutedEventArgs e)
    {
        if (_busy || _source is null) return;
        string source = _source;
        string destination = DestinationBox.Text.Trim();
        string language = _text.LanguageCode;
        long split = long.Parse(((ComboBoxItem)SplitBox.SelectedItem).Tag.ToString()!, CultureInfo.InvariantCulture);
        _output = null;
        _lastProgress = null;
        _lastResult = null;
        _closeRequested = false;
        ClearError();
        OpenButton.Visibility = ReportButton.Visibility = Visibility.Collapsed;
        _cancellation = new CancellationTokenSource();
        _state = ViewState.Scan;
        _hasRun = true;
        _clock.Restart();
        SetBusy(true);
        _timer.Start();
        var progress = new Progress<JobProgress>(UpdateProgress);
        try
        {
            JobResult result = await Task.Run(() => new ConversionService(language).Convert(source, destination, split,
                p => ((IProgress<JobProgress>)progress).Report(p), _cancellation.Token));
            _lastResult = result;
            _output = result.Directory;
            _state = result.Report.Cancelled ? ViewState.Cancelled : ViewState.Done;
            if (!result.Report.Cancelled && result.Report.SkippedCount > 0)
                ShowError("error.skipped", result.Report.SkippedCount);
            OpenButton.Visibility = ReportButton.Visibility = Visibility.Visible;
        }
        catch (OperationCanceledException) { _state = ViewState.ScanCancelled; }
        catch (Exception ex)
        {
            _state = ViewState.Failed;
            if (ex is ConversionFailedException failed)
            {
                _output = failed.OutputDirectory;
                OpenButton.Visibility = Visibility.Visible;
                if (File.Exists(Path.Combine(_output, "rapporto.txt"))) ReportButton.Visibility = Visibility.Visible;
            }
            ShowError(ex);
        }
        finally
        {
            _timer.Stop();
            _clock.Stop();
            _cancellation.Dispose();
            _cancellation = null;
            SetBusy(false);
        }
    }

    private void UpdateProgress(JobProgress progress)
    {
        if (!_busy || _cancellation?.IsCancellationRequested == true) return;
        _lastProgress = progress;
        if (progress.Phase == "scan") _state = ViewState.Scan;
        if (progress.Phase == "convert") _state = ViewState.Convert;
        RenderState();
    }

    private void RenderState()
    {
        string title = "status.idleTitle", detail = "status.idleDetail";
        object?[] arguments = [];
        var progress = _lastProgress;
        var result = _lastResult;
        switch (_state)
        {
            case ViewState.Ready: title = "status.readyTitle"; detail = "status.readyDetail"; break;
            case ViewState.Scan:
                title = "status.scanTitle"; detail = "status.scanDetail"; arguments = [FormatBytes(progress?.Bytes ?? 0)]; break;
            case ViewState.Convert:
                title = progress?.Percent >= 99.9 ? "status.finalizeTitle" : "status.convertTitle";
                detail = "status.convertDetail";
                arguments = [(long)(progress?.Converted ?? 0) + (progress?.Skipped ?? 0), progress?.Total ?? 0, progress?.Skipped ?? 0, progress?.Warnings ?? 0]; break;
            case ViewState.Done when result is not null:
                title = result.Report.SkippedCount > 0 || result.Report.WarningCount > 0 ? "status.doneWarningsTitle" : "status.doneTitle";
                detail = "status.doneDetail"; arguments = [result.Report.ConvertedCount, result.Report.OutputFiles.Count]; break;
            case ViewState.Cancelled when result is not null:
                title = "status.cancelledTitle";
                detail = result.Report.OutputFiles.Count == 0 ? "status.cancelledNoParts" : "status.cancelledParts";
                arguments = result.Report.OutputFiles.Count == 0 ? [result.Report.DiscardedCount]
                    : [result.Report.ConvertedCount, result.Report.OutputFiles.Count, result.Report.DiscardedCount]; break;
            case ViewState.ScanCancelled: title = "status.scanCancelledTitle"; detail = "status.scanCancelledDetail"; break;
            case ViewState.Failed: title = "status.failedTitle"; detail = _output is null ? "status.failedDetail" : "status.failedPartial"; break;
            case ViewState.Cancelling:
                title = "status.cancellingTitle";
                detail = _closeRequested ? "status.closeWait" : progress?.Phase == "scan" ? "status.scanDetail" : "status.convertDetail";
                arguments = _closeRequested ? [] : progress?.Phase == "scan" ? [FormatBytes(progress.Bytes)]
                    : [(long)(progress?.Converted ?? 0) + (progress?.Skipped ?? 0), progress?.Total ?? 0, progress?.Skipped ?? 0, progress?.Warnings ?? 0]; break;
        }
        StatusTitle.Text = _text[title];
        StatusDetail.Text = _text.Format(detail, arguments);
        JobBar.IsIndeterminate = _busy && progress?.Percent is null;
        JobBar.Value = _state == ViewState.Done ? 100 : progress?.Percent ?? 0;
        PercentLabel.Text = _state is ViewState.Cancelled or ViewState.ScanCancelled ? _text["status.cancelledLabel"]
            : _state == ViewState.Failed ? _text["status.errorLabel"]
            : _state == ViewState.Done ? _text.Format("status.percent", 100)
            : _busy ? _text.Format("status.percent", progress?.Percent ?? 0) : "—";
        StartButton.Content = _text[_hasRun ? "button.again" : "button.start"];
        CancelButton.Content = _text[_cancellation?.IsCancellationRequested == true ? "button.closing" : "button.cancel"];
        RenderStats();
    }

    private void RenderStats()
    {
        StatsLabel.Visibility = _hasRun ? Visibility.Visible : Visibility.Collapsed;
        if (!_busy && _lastResult is { } result)
        {
            StatsLabel.Text = _text.Format("stats.result", FormatTime(result.Seconds), result.Report.ConvertedCount,
                result.Report.DiscardedCount, result.Report.WarningCount, result.Report.SkippedCount);
            return;
        }
        string text = _text.Format("stats.elapsed", FormatTime(_clock.Elapsed.TotalSeconds));
        if (_lastProgress is { Phase: "convert", Converted: > 0 } p) text += _text.Format("stats.bytes", FormatBytes(p.Bytes));
        if (_cancellation?.IsCancellationRequested == true) text += _text["stats.closing"];
        StatsLabel.Text = text;
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        SourceButton.IsEnabled = DestinationButton.IsEnabled = DestinationBox.IsEnabled = SplitBox.IsEnabled = LanguageBox.IsEnabled = !busy;
        StartButton.Visibility = busy ? Visibility.Collapsed : Visibility.Visible;
        CancelButton.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        CancelButton.IsEnabled = busy && _cancellation?.IsCancellationRequested != true;
        UpdateStart();
        RenderState();
    }

    private void CancelConversion(object sender, RoutedEventArgs e)
    {
        _cancellation?.Cancel();
        CancelButton.IsEnabled = false;
        _state = ViewState.Cancelling;
        RenderState();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!_busy) return;
        e.Cancel = true;
        _closeRequested = true;
        CancelConversion(this, new RoutedEventArgs());
    }

    private void OpenOutput(object sender, RoutedEventArgs e) { if (_output is not null) OpenPath(_output); }
    private void OpenReport(object sender, RoutedEventArgs e) { if (_output is not null) OpenPath(Path.Combine(_output, "rapporto.txt")); }
    private void OpenPath(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex) { ShowError("error.openPath", ex.Message); }
    }

    private void ClearError() { _errorException = null; _errorKey = null; RenderError(); }
    private void ShowError(Exception exception) { _errorException = exception; _errorKey = null; RenderError(); }
    private void ShowError(string key, params object?[] arguments) { _errorException = null; _errorKey = key; _errorArguments = arguments; RenderError(); }
    private void RenderError()
    {
        ErrorLabel.Visibility = _errorException is not null || _errorKey is not null ? Visibility.Visible : Visibility.Collapsed;
        ErrorLabel.Text = _errorException is not null ? _text.DescribeError(_errorException)
            : _errorKey is not null ? _text.Format(_errorKey, _errorArguments) : "";
    }

    private void ShowAbout(object sender, RoutedEventArgs e) => MessageBox.Show(this,
        _text.Format("about.body", ProductInfo.Name, ProductInfo.Version), _text.Format("about.title", ProductInfo.Name),
        MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK,
        _text.IsRightToLeft ? MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign : MessageBoxOptions.None);

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = !_busy && e.Data.GetData(DataFormats.FileDrop) is string[] { Length: 1 } files && File.Exists(files[0]) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (!_busy && e.Data.GetData(DataFormats.FileDrop) is string[] { Length: 1 } files) SetSource(files[0]);
    }

    private string FormatBytes(long bytes)
    {
        string[] units = ["B", "KiB", "MiB", "GiB", "TiB"];
        double value = bytes; int i = 0;
        while (value >= 1024 && i < units.Length - 1) { value /= 1024; i++; }
        return value.ToString("0.#", _text.Culture) + " " + units[i];
    }

    private string FormatTime(double seconds) => TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss", _text.Culture);
}
