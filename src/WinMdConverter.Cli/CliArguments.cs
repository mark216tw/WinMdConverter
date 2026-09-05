using System.Globalization;
using WinMdConverter.Core;

namespace WinMdConverter.Cli;

public sealed record CliArguments
{
    public string? InputPath { get; init; }
    public string? OutputDirectory { get; init; }
    public OutputFormat Format { get; init; } = OutputFormat.Both;
    public PageOrientation Orientation { get; init; } = PageOrientation.Portrait;
    public decimal? ScalePercent { get; init; }
    public MarginMode MarginMode { get; init; } = MarginMode.Default;
    public PageMargins? CustomMargins { get; init; }
    public string? FontFamily { get; init; }
    public bool IncludeTableOfContents { get; init; }
    public bool Overwrite { get; init; }
    public bool OpenFiles { get; init; }
    public bool Quiet { get; init; }
    public bool Json { get; init; }
    public bool ShowHelp { get; init; }
    public bool ShowVersion { get; init; }
    public bool ListFonts { get; init; }

    public ConversionOptions ToConversionOptions()
    {
        var input = Path.GetFullPath(InputPath ?? throw new ArgumentException("請指定 Markdown 檔案。"));
        return new ConversionOptions
        {
            InputPath = input,
            OutputDirectory = Path.GetFullPath(OutputDirectory ?? Path.GetDirectoryName(input)!),
            Format = Format,
            Orientation = Orientation,
            ScalePercent = ScalePercent,
            MarginMode = MarginMode,
            CustomMargins = CustomMargins,
            FontFamily = FontFamily,
            IncludeTableOfContents = IncludeTableOfContents,
            Overwrite = Overwrite
        };
    }

    public static CliArguments Parse(IReadOnlyList<string> arguments)
    {
        string? input = null;
        string? output = null;
        var format = OutputFormat.Both;
        var orientation = PageOrientation.Portrait;
        decimal? scale = null;
        var marginMode = MarginMode.Default;
        decimal? top = null;
        decimal? right = null;
        decimal? bottom = null;
        decimal? left = null;
        string? font = null;
        var toc = false;
        var overwrite = false;
        var open = false;
        var quiet = false;
        var json = false;
        var help = false;
        var version = false;
        var listFonts = false;

        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            switch (argument)
            {
                case "--format":
                    format = ParseFormat(NextValue(arguments, ref index, argument));
                    break;
                case "--output":
                case "-o":
                    output = NextValue(arguments, ref index, argument);
                    break;
                case "--orientation":
                    orientation = ParseOrientation(NextValue(arguments, ref index, argument));
                    break;
                case "--scale":
                    scale = ParseScale(NextValue(arguments, ref index, argument));
                    break;
                case "--margin":
                    marginMode = ParseMarginMode(NextValue(arguments, ref index, argument));
                    break;
                case "--margin-top":
                    top = ParseDecimal(NextValue(arguments, ref index, argument), argument);
                    break;
                case "--margin-right":
                    right = ParseDecimal(NextValue(arguments, ref index, argument), argument);
                    break;
                case "--margin-bottom":
                    bottom = ParseDecimal(NextValue(arguments, ref index, argument), argument);
                    break;
                case "--margin-left":
                    left = ParseDecimal(NextValue(arguments, ref index, argument), argument);
                    break;
                case "--font":
                    font = NextValue(arguments, ref index, argument);
                    break;
                case "--toc":
                    toc = true;
                    break;
                case "--overwrite":
                    overwrite = true;
                    break;
                case "--open":
                    open = true;
                    break;
                case "--quiet":
                    quiet = true;
                    break;
                case "--json":
                    json = true;
                    break;
                case "--list-fonts":
                    listFonts = true;
                    break;
                case "--version":
                    version = true;
                    break;
                case "--help":
                case "-h":
                case "-?":
                    help = true;
                    break;
                default:
                    if (argument.StartsWith("-", StringComparison.Ordinal))
                        throw new ArgumentException($"不支援的參數：{argument}");
                    if (input is not null)
                        throw new ArgumentException("一次只能轉換一個 Markdown 檔案。");
                    input = argument;
                    break;
            }
        }

        var hasAnyCustomMargin = top is not null || right is not null || bottom is not null || left is not null;
        PageMargins? customMargins = null;
        if (hasAnyCustomMargin)
        {
            if (top is null || right is null || bottom is null || left is null)
                throw new ArgumentException("使用自訂邊界時必須同時指定上、下、左、右四個數值。");
            customMargins = new PageMargins(top.Value, right.Value, bottom.Value, left.Value);
        }

        if (marginMode == MarginMode.Custom && customMargins is null)
            throw new ArgumentException("--margin custom 需要四個 --margin-* 參數。");

        return new CliArguments
        {
            InputPath = input,
            OutputDirectory = output,
            Format = format,
            Orientation = orientation,
            ScalePercent = scale,
            MarginMode = marginMode,
            CustomMargins = customMargins,
            FontFamily = font,
            IncludeTableOfContents = toc,
            Overwrite = overwrite,
            OpenFiles = open,
            Quiet = quiet,
            Json = json,
            ShowHelp = help,
            ShowVersion = version,
            ListFonts = listFonts
        };
    }

    private static string NextValue(IReadOnlyList<string> arguments, ref int index, string option)
    {
        if (++index >= arguments.Count || arguments[index].StartsWith("-", StringComparison.Ordinal))
            throw new ArgumentException($"{option} 缺少設定值。");
        return arguments[index];
    }

    private static OutputFormat ParseFormat(string value) => value.ToLowerInvariant() switch
    {
        "html" => OutputFormat.Html,
        "pdf" => OutputFormat.Pdf,
        "both" => OutputFormat.Both,
        _ => throw new ArgumentException("--format 必須是 html、pdf 或 both。")
    };

    private static PageOrientation ParseOrientation(string value) => value.ToLowerInvariant() switch
    {
        "portrait" => PageOrientation.Portrait,
        "landscape" => PageOrientation.Landscape,
        _ => throw new ArgumentException("--orientation 必須是 portrait 或 landscape。")
    };

    private static decimal? ParseScale(string value)
    {
        if (value.Equals("fit", StringComparison.OrdinalIgnoreCase))
            return null;
        return ParseDecimal(value.TrimEnd('%'), "--scale");
    }

    private static MarginMode ParseMarginMode(string value) => value.ToLowerInvariant() switch
    {
        "default" => MarginMode.Default,
        "none" => MarginMode.None,
        "minimum" => MarginMode.Minimum,
        "custom" => MarginMode.Custom,
        _ => throw new ArgumentException("--margin 必須是 default、none、minimum 或 custom。")
    };

    private static decimal ParseDecimal(string value, string option)
    {
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            throw new ArgumentException($"{option} 必須是有效數字。");
        return parsed;
    }
}
