using WinMdConverter.Cli;
using WinMdConverter.Core;

namespace WinMdConverter.Tests;

public sealed class CliArgumentsTests
{
    [Fact]
    public void Parse_ReadsCompleteConversionCommand()
    {
        var arguments = CliArguments.Parse([
            "document.md", "--format", "pdf", "--orientation", "landscape",
            "--scale", "90", "--margin", "custom", "--margin-top", "10",
            "--margin-right", "11", "--margin-bottom", "12", "--margin-left", "13",
            "--font", "Microsoft JhengHei", "--toc", "--overwrite"
        ]);

        Assert.Equal(OutputFormat.Pdf, arguments.Format);
        Assert.Equal(PageOrientation.Landscape, arguments.Orientation);
        Assert.Equal(90, arguments.ScalePercent);
        Assert.Equal(new PageMargins(10, 11, 12, 13), arguments.CustomMargins);
        Assert.Equal("Microsoft JhengHei", arguments.FontFamily);
        Assert.True(arguments.IncludeTableOfContents);
        Assert.True(arguments.Overwrite);
    }

    [Fact]
    public void Parse_RejectsIncompleteCustomMargins()
    {
        var exception = Assert.Throws<ArgumentException>(() => CliArguments.Parse([
            "document.md", "--margin", "custom", "--margin-top", "10"
        ]));

        Assert.Contains("同時指定", exception.Message);
    }

    [Theory]
    [InlineData("docx", OutputFormat.Docx)]
    [InlineData("html,pdf", OutputFormat.Html | OutputFormat.Pdf)]
    [InlineData("PDF, DOCX", OutputFormat.Pdf | OutputFormat.Docx)]
    [InlineData("all", OutputFormat.All)]
    public void Parse_ReadsSingleCombinedAndAllFormats(string value, OutputFormat expected)
    {
        var arguments = CliArguments.Parse(["document.md", "--format", value]);

        Assert.Equal(expected, arguments.Format);
    }

    [Theory]
    [InlineData("both", "不支援")]
    [InlineData("html,html", "重複")]
    [InlineData("all,pdf", "不可與其他格式混用")]
    [InlineData("html,", "不可為空")]
    public void Parse_RejectsInvalidFormats(string value, string expectedMessage)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            CliArguments.Parse(["document.md", "--format", value]));

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Fact]
    public void Parse_RequiresFormatForConversion()
    {
        var exception = Assert.Throws<ArgumentException>(() => CliArguments.Parse(["document.md"]));

        Assert.Contains("請指定 --format", exception.Message);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("--version")]
    [InlineData("--list-fonts")]
    public void Parse_DoesNotRequireFormatForInformationalCommands(string command)
    {
        var exception = Record.Exception(() => CliArguments.Parse([command]));

        Assert.Null(exception);
    }
}
