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
}
