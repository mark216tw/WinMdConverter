using WinMdConverter.Core;

namespace WinMdConverter.Tests;

public sealed class ConversionOptionsValidatorTests
{
    [Fact]
    public void Validate_RejectsInvalidScaleAndMargins()
    {
        using var directory = new TemporaryDirectory();
        var input = Path.Combine(directory.Path, "document.md");
        File.WriteAllText(input, "# Test");
        var options = new ConversionOptions
        {
            InputPath = input,
            OutputDirectory = directory.Path,
            Format = OutputFormat.Html,
            ScalePercent = 201,
            MarginMode = MarginMode.Custom,
            CustomMargins = new PageMargins(-1, 0, 0, 51)
        };

        var errors = ConversionOptionsValidator.Validate(options);

        Assert.Contains(errors, error => error.Contains("縮放比例", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("自訂邊界", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ConvertAsync_WritesStandaloneHtml()
    {
        using var directory = new TemporaryDirectory();
        var input = Path.Combine(directory.Path, "document.md");
        File.WriteAllText(input, "# 測試文件");
        var options = new ConversionOptions
        {
            InputPath = input,
            OutputDirectory = directory.Path,
            Format = OutputFormat.Html
        };

        var result = await new MarkdownConversionService().ConvertAsync(options);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.HtmlPath);
        Assert.True(File.Exists(result.HtmlPath));
        Assert.Contains("<!doctype html>", await File.ReadAllTextAsync(result.HtmlPath));
    }
}
