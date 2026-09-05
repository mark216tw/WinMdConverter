using WinMdConverter.Core;

namespace WinMdConverter.Tests;

public sealed class HtmlDocumentBuilderTests
{
    [Fact]
    public void Build_RendersAdvancedMarkdownAndTableOfContents()
    {
        var options = CreateOptions(includeToc: true);
        var markdown = """
            # 文件標題

            ## 表格

            | 名稱 | 數量 |
            | --- | ---: |
            | 項目 | 2 |

            - [x] 完成
            """;

        var result = new HtmlDocumentBuilder().Build(markdown, options.InputPath, options);

        Assert.Contains("<nav class=\"toc\"", result.Html);
        Assert.Contains("href=\"#文件標題\"", result.Html);
        Assert.Contains("id=\"文件標題\"", result.Html);
        Assert.Contains("<table>", result.Html);
        Assert.Contains("type=\"checkbox\"", result.Html);
    }

    [Fact]
    public void Build_AssignsUniqueIdsToDuplicateHeadings()
    {
        var options = CreateOptions(includeToc: true);

        var result = new HtmlDocumentBuilder().Build("# 相同\n\n# 相同", options.InputPath, options);

        Assert.Contains("id=\"相同\"", result.Html);
        Assert.Contains("id=\"相同-1\"", result.Html);
    }

    [Fact]
    public void Build_DisablesRawHtml()
    {
        var options = CreateOptions(includeToc: false);

        var result = new HtmlDocumentBuilder().Build("# Safe\n\n<script>alert('x')</script>", options.InputPath, options);

        Assert.DoesNotContain("<script>", result.Html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_EmbedsLocalImage()
    {
        using var directory = new TemporaryDirectory();
        var inputPath = Path.Combine(directory.Path, "document.md");
        File.WriteAllBytes(Path.Combine(directory.Path, "sample.png"), [137, 80, 78, 71]);
        var options = CreateOptions(false) with { InputPath = inputPath, OutputDirectory = directory.Path };

        var result = new HtmlDocumentBuilder().Build("![圖](sample.png)", inputPath, options);

        Assert.Contains("data:image/png;base64,", result.Html);
        Assert.Empty(result.Warnings);
    }

    private static ConversionOptions CreateOptions(bool includeToc) => new()
    {
        InputPath = Path.Combine(Path.GetTempPath(), "document.md"),
        OutputDirectory = Path.GetTempPath(),
        Format = OutputFormat.Html,
        IncludeTableOfContents = includeToc
    };
}
