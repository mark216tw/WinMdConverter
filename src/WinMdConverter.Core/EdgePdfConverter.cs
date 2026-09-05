using System.Diagnostics;

namespace WinMdConverter.Core;

public sealed class EdgePdfConverter
{
    public string? FindEdgePath()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "Application", "msedge.exe")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    public async Task ConvertAsync(string htmlPath, string pdfPath, CancellationToken cancellationToken)
    {
        var edgePath = FindEdgePath() ?? throw new InvalidOperationException("找不到 Microsoft Edge，無法產生 PDF。");
        var profileDirectory = Path.Combine(Path.GetTempPath(), $"WinMdConverter-{Guid.NewGuid():N}");
        Directory.CreateDirectory(profileDirectory);

        try
        {
            var startInfo = new ProcessStartInfo(edgePath)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            startInfo.ArgumentList.Add("--headless=new");
            startInfo.ArgumentList.Add("--disable-gpu");
            startInfo.ArgumentList.Add("--disable-extensions");
            startInfo.ArgumentList.Add("--no-first-run");
            startInfo.ArgumentList.Add("--no-pdf-header-footer");
            startInfo.ArgumentList.Add("--allow-file-access-from-files");
            startInfo.ArgumentList.Add($"--user-data-dir={profileDirectory}");
            startInfo.ArgumentList.Add($"--print-to-pdf={pdfPath}");
            startInfo.ArgumentList.Add(new Uri(Path.GetFullPath(htmlPath)).AbsoluteUri);

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("無法啟動 Microsoft Edge。");
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
                throw;
            }

            var error = await errorTask;
            await outputTask;
            if (process.ExitCode != 0 || !File.Exists(pdfPath) || new FileInfo(pdfPath).Length == 0)
                throw new InvalidOperationException($"Microsoft Edge 產生 PDF 失敗。{Environment.NewLine}{error}".Trim());
        }
        finally
        {
            try
            {
                Directory.Delete(profileDirectory, recursive: true);
            }
            catch (IOException)
            {
                // Edge may release profile files shortly after its parent process exits.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
