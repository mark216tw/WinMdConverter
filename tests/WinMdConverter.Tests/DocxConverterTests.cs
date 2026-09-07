using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using WinMdConverter.Core;

namespace WinMdConverter.Tests;

public sealed class DocxConverterTests
{
    [Fact]
    public async Task ConvertAsync_WritesStructuredDocxWithDocumentSettings()
    {
        using var directory = new TemporaryDirectory();
        var input = Path.Combine(directory.Path, "document.md");
        var imagePath = Path.Combine(directory.Path, "pixel.png");
        await File.WriteAllBytesAsync(imagePath, Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="));
        await File.WriteAllTextAsync(input, """
            # 文件標題

            ## 內容

            這是含有 [連結](https://example.com) 的段落。

            - 第一項
            - 第二項

            | 名稱 | 數量 |
            | --- | ---: |
            | 項目 | 2 |

            ![圖片](pixel.png)
            """);
        var options = new ConversionOptions
        {
            InputPath = input,
            OutputDirectory = directory.Path,
            Format = OutputFormat.Docx,
            Orientation = PageOrientation.Landscape,
            MarginMode = MarginMode.Custom,
            CustomMargins = new PageMargins(10, 11, 12, 13),
            FontFamily = "Arial",
            IncludeTableOfContents = true
        };

        var result = await new MarkdownConversionService(fontService: new FontService()).ConvertAsync(options);

        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Errors));
        Assert.NotNull(result.DocxPath);
        Assert.True(File.Exists(result.DocxPath));
        using var document = WordprocessingDocument.Open(result.DocxPath, false);
        var mainPart = document.MainDocumentPart!;
        var body = mainPart.Document!.Body!;
        Assert.Contains("文件標題", body.InnerText);
        Assert.Contains("第一項", body.InnerText);
        Assert.NotEmpty(body.Descendants<Table>());
        Assert.NotEmpty(body.Descendants<Hyperlink>());
        Assert.Contains(body.Descendants<Hyperlink>(), link => link.Anchor is not null);
        Assert.NotEmpty(body.Descendants<BookmarkStart>());
        Assert.NotEmpty(mainPart.ImageParts);

        var section = body.Elements<SectionProperties>().Single();
        var size = section.GetFirstChild<PageSize>()!;
        Assert.Equal(PageOrientationValues.Landscape, size.Orient?.Value);
        var margins = section.GetFirstChild<PageMargin>()!;
        Assert.Equal(567, margins.Top?.Value);
        Assert.Equal(624U, margins.Right?.Value);
        Assert.Equal(680, margins.Bottom?.Value);
        Assert.Equal(737U, margins.Left?.Value);

        var fonts = mainPart.StyleDefinitionsPart!.Styles!.Descendants<RunFonts>().First();
        Assert.Equal("Arial", fonts.EastAsia?.Value);
        var validationErrors = new OpenXmlValidator(DocumentFormat.OpenXml.FileFormatVersions.Office2021)
            .Validate(document)
            .ToArray();
        Assert.True(validationErrors.Length == 0, string.Join(
            Environment.NewLine,
            validationErrors.Select(error => $"{error.Path?.XPath}: {error.Description}")));
    }

    [Fact]
    public async Task ConvertAsync_ReportsUnsupportedDocxImagesAsWarnings()
    {
        using var directory = new TemporaryDirectory();
        var input = Path.Combine(directory.Path, "document.md");
        await File.WriteAllBytesAsync(Path.Combine(directory.Path, "sample.webp"), [1, 2, 3]);
        await File.WriteAllTextAsync(input, "![本機](sample.webp)\n\n![遠端](https://example.com/image.png)");
        var options = new ConversionOptions
        {
            InputPath = input,
            OutputDirectory = directory.Path,
            Format = OutputFormat.Docx
        };

        var result = await new MarkdownConversionService().ConvertAsync(options);

        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Errors));
        Assert.Contains(result.Warnings, warning => warning.Contains("WebP", StringComparison.Ordinal));
        Assert.Contains(result.Warnings, warning => warning.Contains("遠端圖片", StringComparison.Ordinal));
    }
}
