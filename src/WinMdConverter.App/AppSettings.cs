using System.IO;
using System.Text.Json;

namespace WinMdConverter.App;

public sealed record AppSettings
{
    public string? OutputDirectory { get; init; }
    public bool OutputHtml { get; init; } = true;
    public bool OutputPdf { get; init; } = true;
    public bool OutputDocx { get; init; } = true;
    public bool Landscape { get; init; }
    public decimal? ScalePercent { get; init; }
    public int MarginMode { get; init; }
    public decimal MarginTop { get; init; } = 20;
    public decimal MarginRight { get; init; } = 20;
    public decimal MarginBottom { get; init; } = 20;
    public decimal MarginLeft { get; init; } = 20;
    public string? FontFamily { get; init; }
    public bool IncludeTableOfContents { get; init; }

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WinMdConverter",
        "settings.json");

    public static AppSettings Load()
    {
        try
        {
            return File.Exists(SettingsPath)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new AppSettings()
                : new AppSettings();
        }
        catch (Exception) when (File.Exists(SettingsPath))
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(directory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }
}
