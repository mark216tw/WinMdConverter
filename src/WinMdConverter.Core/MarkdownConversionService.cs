using System.Text;

namespace WinMdConverter.Core;

public sealed class MarkdownConversionService(
    HtmlDocumentBuilder? htmlBuilder = null,
    EdgePdfConverter? pdfConverter = null,
    DocxConverter? docxConverter = null,
    FontService? fontService = null)
{
    private readonly HtmlDocumentBuilder _htmlBuilder = htmlBuilder ?? new HtmlDocumentBuilder();
    private readonly EdgePdfConverter _pdfConverter = pdfConverter ?? new EdgePdfConverter();
    private readonly DocxConverter _docxConverter = docxConverter ?? new DocxConverter();
    private readonly FontService _fontService = fontService ?? new FontService();

    public async Task<ConversionResult> ConvertAsync(
        ConversionOptions options,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ConversionOptionsValidator.Validate(options, _fontService);
        if (validationErrors.Count > 0)
            throw new ConversionValidationException(validationErrors);

        var effectiveOptions = string.IsNullOrWhiteSpace(options.FontFamily)
            ? options
            : options with { FontFamily = _fontService.ResolveFamily(options.FontFamily) };

        Directory.CreateDirectory(options.OutputDirectory);
        var baseName = Path.GetFileNameWithoutExtension(options.InputPath);
        var htmlOutputPath = Path.Combine(options.OutputDirectory, $"{baseName}.html");
        var pdfOutputPath = Path.Combine(options.OutputDirectory, $"{baseName}.pdf");
        var docxOutputPath = Path.Combine(options.OutputDirectory, $"{baseName}.docx");
        EnsureCanWrite(options, htmlOutputPath, pdfOutputPath, docxOutputPath);

        progress?.Report(new(10, "讀取 Markdown"));
        var markdown = await File.ReadAllTextAsync(options.InputPath, Encoding.UTF8, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        progress?.Report(new(30, "解析文件內容"));
        var built = _htmlBuilder.Build(markdown, options.InputPath, effectiveOptions);
        var warnings = built.Warnings.ToList();
        var errors = new List<string>();
        string? completedHtmlPath = null;
        string? completedPdfPath = null;
        string? completedDocxPath = null;
        string? temporaryHtmlPath = null;

        try
        {
            progress?.Report(new(50, "產生 HTML"));
            if (options.Format.HasFlag(OutputFormat.Html))
            {
                await WriteAtomicallyAsync(htmlOutputPath, built.Html, cancellationToken);
                completedHtmlPath = htmlOutputPath;
                progress?.Report(new(70, "儲存 HTML"));
            }

            if (options.Format.HasFlag(OutputFormat.Pdf))
            {
                var htmlForPdf = completedHtmlPath;
                if (htmlForPdf is null)
                {
                    temporaryHtmlPath = Path.Combine(Path.GetTempPath(), $"WinMdConverter-{Guid.NewGuid():N}.html");
                    await File.WriteAllTextAsync(temporaryHtmlPath, built.Html, new UTF8Encoding(false), cancellationToken);
                    htmlForPdf = temporaryHtmlPath;
                }

                progress?.Report(new(65, "啟動 Microsoft Edge"));
                var temporaryPdfPath = Path.Combine(options.OutputDirectory, $".{baseName}.{Guid.NewGuid():N}.tmp.pdf");
                try
                {
                    await _pdfConverter.ConvertAsync(htmlForPdf, temporaryPdfPath, cancellationToken);
                    File.Move(temporaryPdfPath, pdfOutputPath, options.Overwrite);
                    completedPdfPath = pdfOutputPath;
                    progress?.Report(new(80, "產生 PDF"));
                }
                catch (OperationCanceledException)
                {
                    TryDelete(temporaryPdfPath);
                    throw;
                }
                catch (Exception exception)
                {
                    TryDelete(temporaryPdfPath);
                    errors.Add(exception.Message);
                }
            }

            if (options.Format.HasFlag(OutputFormat.Docx))
            {
                progress?.Report(new(85, "產生 DOCX"));
                try
                {
                    await _docxConverter.ConvertAsync(built.Html, docxOutputPath, effectiveOptions, cancellationToken);
                    completedDocxPath = docxOutputPath;
                    progress?.Report(new(95, "儲存 DOCX"));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    errors.Add($"DOCX：{exception.Message}");
                }
            }
        }
        finally
        {
            if (temporaryHtmlPath is not null)
                TryDelete(temporaryHtmlPath);
        }

        var result = new ConversionResult(completedHtmlPath, completedPdfPath, completedDocxPath, warnings, errors);
        progress?.Report(new(100, result.IsSuccess ? "轉換完成" : result.IsPartial ? "部分轉換完成" : "轉換失敗"));
        return result;
    }

    private static void EnsureCanWrite(ConversionOptions options, string htmlPath, string pdfPath, string docxPath)
    {
        var conflicts = new List<string>();
        if (!options.Overwrite && options.Format.HasFlag(OutputFormat.Html) && File.Exists(htmlPath))
            conflicts.Add(htmlPath);
        if (!options.Overwrite && options.Format.HasFlag(OutputFormat.Pdf) && File.Exists(pdfPath))
            conflicts.Add(pdfPath);
        if (!options.Overwrite && options.Format.HasFlag(OutputFormat.Docx) && File.Exists(docxPath))
            conflicts.Add(docxPath);

        if (conflicts.Count > 0)
            throw new IOException($"輸出檔案已存在：{string.Join(", ", conflicts)}");
    }

    private static async Task WriteAtomicallyAsync(string outputPath, string content, CancellationToken cancellationToken)
    {
        var temporaryPath = Path.Combine(Path.GetDirectoryName(outputPath)!, $".{Path.GetFileName(outputPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllTextAsync(temporaryPath, content, new UTF8Encoding(false), cancellationToken);
            File.Move(temporaryPath, outputPath, overwrite: true);
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
