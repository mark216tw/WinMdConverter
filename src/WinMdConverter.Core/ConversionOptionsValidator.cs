namespace WinMdConverter.Core;

public static class ConversionOptionsValidator
{
    public static IReadOnlyList<string> Validate(ConversionOptions options, FontService? fontService = null)
    {
        var errors = new List<string>();
        var extension = Path.GetExtension(options.InputPath);

        if (string.IsNullOrWhiteSpace(options.InputPath) || !File.Exists(options.InputPath))
            errors.Add("找不到 Markdown 檔案。");
        else if (!extension.Equals(".md", StringComparison.OrdinalIgnoreCase) &&
                 !extension.Equals(".markdown", StringComparison.OrdinalIgnoreCase))
            errors.Add("來源檔案必須是 .md 或 .markdown。");

        if (string.IsNullOrWhiteSpace(options.OutputDirectory))
            errors.Add("請指定輸出資料夾。");

        if (options.Format == OutputFormat.None)
            errors.Add("請至少選擇一種輸出格式。");
        else if ((options.Format & ~OutputFormat.All) != 0)
            errors.Add("輸出格式包含不支援的值。");

        if (options.ScalePercent is < 50 or > 200)
            errors.Add("自訂縮放比例必須介於 50% 到 200%。");

        if (options.MarginMode == MarginMode.Custom)
        {
            if (options.CustomMargins is null)
            {
                errors.Add("自訂邊界需要指定上、下、左、右數值。");
            }
            else if (new[] { options.CustomMargins.Top, options.CustomMargins.Right, options.CustomMargins.Bottom, options.CustomMargins.Left }
                     .Any(value => value is < 0 or > 50))
            {
                errors.Add("自訂邊界必須介於 0 mm 到 50 mm。");
            }
        }

        if (!string.IsNullOrWhiteSpace(options.FontFamily) &&
            fontService is not null &&
            !fontService.Exists(options.FontFamily))
        {
            errors.Add($"Windows 中找不到字型「{options.FontFamily}」。");
        }

        return errors;
    }

    public static PageMargins ResolveMargins(ConversionOptions options) => options.MarginMode switch
    {
        MarginMode.None => PageMargins.None,
        MarginMode.Minimum => PageMargins.Minimum,
        MarginMode.Custom => options.CustomMargins ?? PageMargins.Default,
        _ => PageMargins.Default
    };
}
