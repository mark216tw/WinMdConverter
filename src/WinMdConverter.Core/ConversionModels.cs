namespace WinMdConverter.Core;

[Flags]
public enum OutputFormat
{
    Html = 1,
    Pdf = 2,
    Both = Html | Pdf
}

public enum PageOrientation
{
    Portrait,
    Landscape
}

public enum MarginMode
{
    Default,
    None,
    Minimum,
    Custom
}

public sealed record PageMargins(decimal Top, decimal Right, decimal Bottom, decimal Left)
{
    public static PageMargins Default { get; } = new(20, 20, 20, 20);
    public static PageMargins None { get; } = new(0, 0, 0, 0);
    public static PageMargins Minimum { get; } = new(5, 5, 5, 5);
}

public sealed record ConversionOptions
{
    public required string InputPath { get; init; }
    public required string OutputDirectory { get; init; }
    public OutputFormat Format { get; init; } = OutputFormat.Both;
    public PageOrientation Orientation { get; init; } = PageOrientation.Portrait;
    public decimal? ScalePercent { get; init; }
    public MarginMode MarginMode { get; init; } = MarginMode.Default;
    public PageMargins? CustomMargins { get; init; }
    public string? FontFamily { get; init; }
    public bool IncludeTableOfContents { get; init; }
    public bool Overwrite { get; init; }
}

public sealed record ConversionProgress(int Percentage, string Message);

public sealed record ConversionResult(
    string? HtmlPath,
    string? PdfPath,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors)
{
    public bool IsSuccess => Errors.Count == 0;
    public bool IsPartial => Errors.Count > 0 && (HtmlPath is not null || PdfPath is not null);
}

public sealed class ConversionValidationException(IReadOnlyList<string> errors)
    : Exception(string.Join(Environment.NewLine, errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}
