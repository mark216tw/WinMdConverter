using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using WinMdConverter.Core;

namespace WinMdConverter.Cli;

public static class CliApplication
{
    public static async Task<int> RunAsync(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        CliArguments parsed;
        try
        {
            parsed = CliArguments.Parse(args);
        }
        catch (ArgumentException exception)
        {
            WriteError(exception.Message, json: args.Contains("--json", StringComparer.Ordinal));
            return 1;
        }

        if (parsed.ShowHelp)
        {
            PrintHelp();
            return 0;
        }

        if (parsed.ShowVersion)
        {
            Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0");
            return 0;
        }

        var fontService = new FontService();
        if (parsed.ListFonts)
        {
            foreach (var family in fontService.GetInstalledFamilies())
                Console.WriteLine(family);
            return 0;
        }

        ConversionOptions options;
        try
        {
            options = parsed.ToConversionOptions();
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            WriteError(exception.Message, parsed.Json);
            return 1;
        }

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

        var progress = parsed.Quiet || parsed.Json
            ? null
            : new InlineProgress<ConversionProgress>(value => Console.Error.WriteLine($"[{value.Percentage,3}%] {value.Message}"));

        try
        {
            if (options.Format.HasFlag(OutputFormat.Pdf) && new EdgePdfConverter().FindEdgePath() is null)
            {
                WriteError("找不到 Microsoft Edge，無法產生 PDF。", parsed.Json);
                return 6;
            }

            var service = new MarkdownConversionService(fontService: fontService);
            var result = await service.ConvertAsync(options, progress, cancellation.Token);
            WriteResult(result, parsed.Json, parsed.Quiet);

            if (parsed.OpenFiles)
                OpenOutputs(result);

            if (result.IsSuccess)
                return 0;
            return result.IsPartial ? 4 : 3;
        }
        catch (OperationCanceledException)
        {
            WriteError("轉換已取消。", parsed.Json);
            return 130;
        }
        catch (ConversionValidationException exception)
        {
            WriteError(string.Join(Environment.NewLine, exception.Errors), parsed.Json);
            return 1;
        }
        catch (IOException exception)
        {
            WriteError(exception.Message, parsed.Json);
            return 5;
        }
        catch (UnauthorizedAccessException exception)
        {
            WriteError(exception.Message, parsed.Json);
            return 5;
        }
        catch (Exception exception)
        {
            WriteError(exception.Message, parsed.Json);
            return 3;
        }
    }

    private static void WriteResult(ConversionResult result, bool json, bool quiet)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                success = result.IsSuccess,
                partial = result.IsPartial,
                htmlPath = result.HtmlPath,
                pdfPath = result.PdfPath,
                docxPath = result.DocxPath,
                warnings = result.Warnings,
                errors = result.Errors
            }, JsonOptions));
            return;
        }

        if (!quiet)
            Console.WriteLine(result.IsSuccess ? "轉換完成" : result.IsPartial ? "部分轉換完成" : "轉換失敗");
        if (!quiet && result.HtmlPath is not null)
            Console.WriteLine($"HTML: {result.HtmlPath}");
        if (!quiet && result.PdfPath is not null)
            Console.WriteLine($"PDF:  {result.PdfPath}");
        if (!quiet && result.DocxPath is not null)
            Console.WriteLine($"DOCX: {result.DocxPath}");
        foreach (var warning in result.Warnings)
            Console.Error.WriteLine($"警告：{warning}");
        foreach (var error in result.Errors)
            Console.Error.WriteLine($"錯誤：{error}");
    }

    private static void WriteError(string message, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { success = false, errors = new[] { message } }, JsonOptions));
            return;
        }

        Console.Error.WriteLine($"錯誤：{message}");
    }

    private static void OpenOutputs(ConversionResult result)
    {
        foreach (var path in new[] { result.HtmlPath, result.PdfPath, result.DocxPath }.Where(path => path is not null))
        {
            Process.Start(new ProcessStartInfo(path!) { UseShellExecute = true });
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            mdconvert - Windows Markdown 轉 HTML / PDF / DOCX 工具

            用法：
              mdconvert <input.md> [options]
              mdconvert --list-fonts

            選項：
              --format <formats>               必填；html、pdf、docx、逗號組合或 all
              -o, --output <directory>         輸出資料夾
              --orientation portrait|landscape 紙張方向，預設 portrait
              --scale fit|50-200               HTML / PDF 縮放模式或百分比，預設 fit
              --margin default|none|minimum|custom
              --margin-top <mm>                自訂上邊界
              --margin-right <mm>              自訂右邊界
              --margin-bottom <mm>             自訂下邊界
              --margin-left <mm>               自訂左邊界
              --font <family>                  Windows 字型家族
              --toc                            在文件開頭顯示目錄
              --overwrite                      覆寫現有檔案
              --open                           完成後開啟檔案
              --quiet                          隱藏進度訊息
              --json                           輸出 JSON 結果
              --list-fonts                     列出已安裝字型
              --version                        顯示版本
              -h, --help                       顯示說明
            """);
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
