using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using WinMdConverter.Core;

namespace WinMdConverter.App;

public partial class MainWindow : Window
{
    private const string SystemDefaultFont = "系統預設";
    private readonly FontService _fontService = new();
    private readonly MarkdownConversionService _conversionService;
    private CancellationTokenSource? _cancellation;
    private string? _htmlPath;
    private string? _pdfPath;

    public MainWindow()
    {
        InitializeComponent();
        _conversionService = new MarkdownConversionService(fontService: _fontService);
        LoadFonts();
        LoadSettings();
    }

    private void LoadFonts()
    {
        var installed = _fontService.GetInstalledFamilies();
        var preferred = new[]
        {
            "微軟正黑體", "Microsoft JhengHei", "Microsoft JhengHei UI", "新細明體",
            "PMingLiU", "細明體", "MingLiU", "標楷體", "DFKai-SB", "Noto Sans TC", "Noto Serif TC"
        };
        var ordered = preferred.Where(name => installed.Contains(name, StringComparer.CurrentCultureIgnoreCase))
            .Concat(installed.Where(name => !preferred.Contains(name, StringComparer.CurrentCultureIgnoreCase)));
        FontComboBox.ItemsSource = new[] { SystemDefaultFont }.Concat(ordered).ToArray();
        FontComboBox.SelectedIndex = 0;
    }

    private void LoadSettings()
    {
        var settings = AppSettings.Load();
        OutputPathBox.Text = settings.OutputDirectory ?? string.Empty;
        HtmlCheckBox.IsChecked = settings.OutputHtml;
        PdfCheckBox.IsChecked = settings.OutputPdf;
        OrientationComboBox.SelectedIndex = settings.Landscape ? 1 : 0;
        ScaleModeComboBox.SelectedIndex = settings.ScalePercent is null ? 0 : 1;
        ScaleBox.Text = (settings.ScalePercent ?? 100).ToString(CultureInfo.CurrentCulture);
        MarginModeComboBox.SelectedIndex = Math.Clamp(settings.MarginMode, 0, 3);
        MarginTopBox.Text = settings.MarginTop.ToString(CultureInfo.CurrentCulture);
        MarginRightBox.Text = settings.MarginRight.ToString(CultureInfo.CurrentCulture);
        MarginBottomBox.Text = settings.MarginBottom.ToString(CultureInfo.CurrentCulture);
        MarginLeftBox.Text = settings.MarginLeft.ToString(CultureInfo.CurrentCulture);
        TocCheckBox.IsChecked = settings.IncludeTableOfContents;

        if (!string.IsNullOrWhiteSpace(settings.FontFamily) && FontComboBox.Items.Contains(settings.FontFamily))
            FontComboBox.SelectedItem = settings.FontFamily;
    }

    private void BrowseInput_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "選擇 Markdown 檔案",
            Filter = "Markdown 文件 (*.md;*.markdown)|*.md;*.markdown|所有檔案 (*.*)|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != true)
            return;

        InputPathBox.Text = dialog.FileName;
        if (string.IsNullOrWhiteSpace(OutputPathBox.Text))
            OutputPathBox.Text = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
    }

    private void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "選擇輸出資料夾",
            InitialDirectory = Directory.Exists(OutputPathBox.Text) ? OutputPathBox.Text : null
        };
        if (dialog.ShowDialog(this) == true)
            OutputPathBox.Text = dialog.FolderName;
    }

    private void ScaleMode_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (ScaleBox is not null)
            ScaleBox.IsEnabled = ScaleModeComboBox.SelectedIndex == 1;
    }

    private void MarginMode_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (CustomMarginsPanel is not null)
            CustomMarginsPanel.IsEnabled = MarginModeComboBox.SelectedIndex == 3;
    }

    private async void Convert_Click(object sender, RoutedEventArgs e)
    {
        ConversionOptions options;
        try
        {
            options = ReadOptions();
            var validation = ConversionOptionsValidator.Validate(options, _fontService);
            if (validation.Count > 0)
                throw new ConversionValidationException(validation);

            if (HasOutputConflicts(options))
            {
                var answer = MessageBox.Show(this, "輸出檔案已存在，是否覆寫？", "確認覆寫", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (answer != MessageBoxResult.Yes)
                    return;
                options = options with { Overwrite = true };
            }
        }
        catch (Exception exception) when (exception is ConversionValidationException or FormatException or IOException)
        {
            MessageBox.Show(this, exception.Message, "設定錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SetBusy(true);
        ResultLinksPanel.Visibility = Visibility.Collapsed;
        MessageText.Visibility = Visibility.Collapsed;
        ConversionProgressBar.Value = 0;
        ProgressText.Text = "0%";
        StatusText.Text = "準備轉換";
        _cancellation = new CancellationTokenSource();
        var progress = new Progress<ConversionProgress>(value =>
        {
            ConversionProgressBar.Value = value.Percentage;
            ProgressText.Text = $"{value.Percentage}%";
            StatusText.Text = value.Message;
        });

        try
        {
            var result = await _conversionService.ConvertAsync(options, progress, _cancellation.Token);
            _htmlPath = result.HtmlPath;
            _pdfPath = result.PdfPath;
            StatusText.Text = result.IsSuccess ? "轉換完成" : "部分轉換完成";
            ShowResult(result);
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "轉換已取消";
            MessageText.Text = "未完成的暫存檔案已清除。";
            MessageText.Visibility = Visibility.Visible;
        }
        catch (Exception exception)
        {
            StatusText.Text = "轉換失敗";
            MessageText.Text = exception.Message;
            MessageText.Visibility = Visibility.Visible;
        }
        finally
        {
            _cancellation.Dispose();
            _cancellation = null;
            SetBusy(false);
        }
    }

    private ConversionOptions ReadOptions()
    {
        decimal? scale = null;
        if (ScaleModeComboBox.SelectedIndex == 1)
            scale = ParseNumber(ScaleBox.Text, "縮放比例");

        PageMargins? margins = null;
        if (MarginModeComboBox.SelectedIndex == 3)
        {
            margins = new PageMargins(
                ParseNumber(MarginTopBox.Text, "上邊界"),
                ParseNumber(MarginRightBox.Text, "右邊界"),
                ParseNumber(MarginBottomBox.Text, "下邊界"),
                ParseNumber(MarginLeftBox.Text, "左邊界"));
        }

        var format = (HtmlCheckBox.IsChecked == true ? OutputFormat.Html : 0) |
                     (PdfCheckBox.IsChecked == true ? OutputFormat.Pdf : 0);
        var selectedFont = FontComboBox.SelectedItem as string;

        return new ConversionOptions
        {
            InputPath = InputPathBox.Text.Trim(),
            OutputDirectory = OutputPathBox.Text.Trim(),
            Format = format,
            Orientation = OrientationComboBox.SelectedIndex == 1 ? PageOrientation.Landscape : PageOrientation.Portrait,
            ScalePercent = scale,
            MarginMode = (MarginMode)Math.Clamp(MarginModeComboBox.SelectedIndex, 0, 3),
            CustomMargins = margins,
            FontFamily = selectedFont == SystemDefaultFont ? null : selectedFont,
            IncludeTableOfContents = TocCheckBox.IsChecked == true,
            Overwrite = false
        };
    }

    private static decimal ParseNumber(string value, string label)
    {
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var result))
            throw new FormatException($"{label}必須是有效數字。");
        return result;
    }

    private static bool HasOutputConflicts(ConversionOptions options)
    {
        var name = Path.GetFileNameWithoutExtension(options.InputPath);
        return options.Format.HasFlag(OutputFormat.Html) && File.Exists(Path.Combine(options.OutputDirectory, $"{name}.html")) ||
               options.Format.HasFlag(OutputFormat.Pdf) && File.Exists(Path.Combine(options.OutputDirectory, $"{name}.pdf"));
    }

    private void ShowResult(ConversionResult result)
    {
        OpenHtmlButton.Visibility = result.HtmlPath is null ? Visibility.Collapsed : Visibility.Visible;
        OpenPdfButton.Visibility = result.PdfPath is null ? Visibility.Collapsed : Visibility.Visible;
        ResultLinksPanel.Visibility = result.HtmlPath is not null || result.PdfPath is not null
            ? Visibility.Visible
            : Visibility.Collapsed;

        var messages = result.Warnings.Select(warning => $"警告：{warning}")
            .Concat(result.Errors.Select(error => $"錯誤：{error}"))
            .ToArray();
        if (messages.Length > 0)
        {
            MessageText.Text = string.Join(Environment.NewLine, messages);
            MessageText.Visibility = Visibility.Visible;
        }
    }

    private void SetBusy(bool busy)
    {
        SettingsPanel.IsEnabled = !busy;
        ConvertButton.IsEnabled = !busy;
        CancelButton.Content = busy ? "取消" : "結束";
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (_cancellation is null)
            Close();
        else
            _cancellation.Cancel();
    }

    private void OpenHtml_Click(object sender, RoutedEventArgs e) => OpenPath(_htmlPath);

    private void OpenPdf_Click(object sender, RoutedEventArgs e) => OpenPath(_pdfPath);

    private void OpenFolder_Click(object sender, RoutedEventArgs e) => OpenPath(OutputPathBox.Text);

    private static void OpenPath(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path) && (File.Exists(path) || Directory.Exists(path)))
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _cancellation?.Cancel();
        try
        {
            var options = ReadOptions();
            AppSettings.Save(new AppSettings
            {
                OutputDirectory = options.OutputDirectory,
                OutputHtml = options.Format.HasFlag(OutputFormat.Html),
                OutputPdf = options.Format.HasFlag(OutputFormat.Pdf),
                Landscape = options.Orientation == PageOrientation.Landscape,
                ScalePercent = options.ScalePercent,
                MarginMode = (int)options.MarginMode,
                MarginTop = options.CustomMargins?.Top ?? 20,
                MarginRight = options.CustomMargins?.Right ?? 20,
                MarginBottom = options.CustomMargins?.Bottom ?? 20,
                MarginLeft = options.CustomMargins?.Left ?? 20,
                FontFamily = options.FontFamily,
                IncludeTableOfContents = options.IncludeTableOfContents
            });
        }
        catch (Exception)
        {
            // Invalid in-progress input should not prevent the window from closing.
        }
    }
}
