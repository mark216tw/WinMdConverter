using System.Globalization;
using System.Net;
using System.Text;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace WinMdConverter.Core;

public sealed record HtmlBuildResult(string Html, IReadOnlyList<string> Warnings);

public sealed class HtmlDocumentBuilder
{
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    public HtmlBuildResult Build(string markdown, string sourcePath, ConversionOptions options)
    {
        var warnings = new List<string>();
        var document = Markdown.Parse(markdown, _pipeline);
        EmbedLocalImages(document, Path.GetDirectoryName(sourcePath)!, options.Format.HasFlag(OutputFormat.Docx), warnings);

        var headings = PrepareHeadings(document);
        var body = Render(document);
        var toc = options.IncludeTableOfContents && headings.Count > 0
            ? BuildTableOfContents(headings)
            : string.Empty;
        var title = headings.FirstOrDefault(item => item.Level == 1)?.Text
            ?? Path.GetFileNameWithoutExtension(sourcePath);

        return new HtmlBuildResult(BuildDocument(title, toc + body, options), warnings);
    }

    private static List<HeadingInfo> PrepareHeadings(MarkdownDocument document)
    {
        var headings = new List<HeadingInfo>();
        var usedIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var heading in document.Descendants<HeadingBlock>())
        {
            var text = ReadInlineText(heading.Inline).Trim();
            if (text.Length == 0)
                continue;

            var baseId = Slugify(text);
            usedIds.TryGetValue(baseId, out var count);
            usedIds[baseId] = count + 1;
            var id = count == 0 ? baseId : $"{baseId}-{count}";
            heading.GetAttributes().Id = id;
            headings.Add(new HeadingInfo(heading.Level, text, id));
        }

        return headings;
    }

    private static string ReadInlineText(ContainerInline? container)
    {
        if (container is null)
            return string.Empty;

        var text = new StringBuilder();
        for (var inline = container.FirstChild; inline is not null; inline = inline.NextSibling)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    text.Append(literal.Content);
                    break;
                case CodeInline code:
                    text.Append(code.Content);
                    break;
                case LineBreakInline:
                    text.Append(' ');
                    break;
                case ContainerInline nested:
                    text.Append(ReadInlineText(nested));
                    break;
            }
        }

        return text.ToString();
    }

    private static string Slugify(string value)
    {
        var slug = new StringBuilder();
        var pendingDash = false;

        foreach (var character in value.Normalize(NormalizationForm.FormKC).ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                if (pendingDash && slug.Length > 0)
                    slug.Append('-');
                slug.Append(character);
                pendingDash = false;
            }
            else if (char.IsWhiteSpace(character) || character is '-' or '_')
            {
                pendingDash = true;
            }
        }

        return slug.Length == 0 ? "section" : slug.ToString();
    }

    private string Render(MarkdownDocument document)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        var renderer = new HtmlRenderer(writer);
        _pipeline.Setup(renderer);
        renderer.Render(document);
        writer.Flush();
        return writer.ToString();
    }

    private static string BuildTableOfContents(IReadOnlyList<HeadingInfo> headings)
    {
        var root = new TocNode(0, string.Empty, string.Empty);
        var stack = new Stack<TocNode>();
        stack.Push(root);

        foreach (var heading in headings)
        {
            while (stack.Count > 1 && stack.Peek().Level >= heading.Level)
                stack.Pop();

            var node = new TocNode(heading.Level, heading.Text, heading.Id);
            stack.Peek().Children.Add(node);
            stack.Push(node);
        }

        var html = new StringBuilder("<nav class=\"toc\" aria-label=\"目錄\"><h2>目錄</h2>");
        AppendTocNodes(html, root.Children);
        html.Append("</nav>");
        return html.ToString();
    }

    private static void AppendTocNodes(StringBuilder html, IReadOnlyList<TocNode> nodes)
    {
        if (nodes.Count == 0)
            return;

        html.Append("<ol>");
        foreach (var node in nodes)
        {
            html.Append("<li><a href=\"#")
                .Append(WebUtility.HtmlEncode(node.Id))
                .Append("\">")
                .Append(WebUtility.HtmlEncode(node.Text))
                .Append("</a>");
            AppendTocNodes(html, node.Children);
            html.Append("</li>");
        }
        html.Append("</ol>");
    }

    private static void EmbedLocalImages(
        MarkdownDocument document,
        string sourceDirectory,
        bool producingDocx,
        ICollection<string> warnings)
    {
        foreach (var image in document.Descendants<LinkInline>().Where(link => link.IsImage && link.Url is not null))
        {
            var url = image.Url!;
            if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri) && absoluteUri.Scheme is "http" or "https")
            {
                if (producingDocx)
                    warnings.Add($"DOCX 不會嵌入遠端圖片：{url}");
                continue;
            }
            if (absoluteUri?.Scheme == "data")
                continue;

            var localValue = Uri.UnescapeDataString(url.Split('#', '?')[0]);
            var path = Path.IsPathRooted(localValue)
                ? localValue
                : Path.GetFullPath(Path.Combine(sourceDirectory, localValue.Replace('/', Path.DirectorySeparatorChar)));

            if (!File.Exists(path))
            {
                warnings.Add($"找不到圖片：{url}");
                continue;
            }

            var mime = GetMimeType(Path.GetExtension(path));
            if (mime is null)
            {
                warnings.Add($"不支援的圖片格式：{url}");
                continue;
            }

            if (producingDocx && mime == "image/webp")
                warnings.Add($"DOCX 不支援 WebP 圖片：{url}");

            image.Url = $"data:{mime};base64,{Convert.ToBase64String(File.ReadAllBytes(path))}";
        }
    }

    private static string? GetMimeType(string extension) => extension.ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".bmp" => "image/bmp",
        ".svg" => "image/svg+xml",
        _ => null
    };

    private static string BuildDocument(string title, string body, ConversionOptions options)
    {
        var margins = ConversionOptionsValidator.ResolveMargins(options);
        var orientation = options.Orientation == PageOrientation.Landscape ? "landscape" : "portrait";
        var zoom = options.ScalePercent is null
            ? "1"
            : (options.ScalePercent.Value / 100m).ToString("0.##", CultureInfo.InvariantCulture);
        var font = string.IsNullOrWhiteSpace(options.FontFamily)
            ? "\"Microsoft JhengHei\", \"Segoe UI\", sans-serif"
            : $"\"{EscapeCss(options.FontFamily)}\", \"Microsoft JhengHei\", sans-serif";

        return $$"""
            <!doctype html>
            <html lang="zh-Hant">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{{WebUtility.HtmlEncode(title)}}</title>
              <style>
                :root { color-scheme: light; }
                @page { size: A4 {{orientation}}; margin: {{Mm(margins.Top)}} {{Mm(margins.Right)}} {{Mm(margins.Bottom)}} {{Mm(margins.Left)}}; }
                * { box-sizing: border-box; }
                html { background: #eef0f3; color: #202124; }
                body { margin: 0 auto; max-width: 920px; padding: 48px 56px; background: #fff; font-family: {{font}}; font-size: 16px; line-height: 1.75; overflow-wrap: anywhere; }
                .document { zoom: {{zoom}}; }
                h1, h2, h3, h4, h5, h6 { color: #172033; line-height: 1.3; margin: 1.5em 0 .65em; break-after: avoid; }
                h1 { font-size: 2em; border-bottom: 2px solid #d7dce5; padding-bottom: .3em; }
                h2 { font-size: 1.55em; border-bottom: 1px solid #e4e7ec; padding-bottom: .25em; }
                a { color: #195eb5; text-decoration-thickness: 1px; text-underline-offset: 2px; }
                img, svg { display: block; max-width: 100%; height: auto; margin: 1.2em auto; }
                table { width: 100%; max-width: 100%; border-collapse: collapse; margin: 1.2em 0; font-size: .94em; }
                th, td { border: 1px solid #cfd5df; padding: .5em .7em; text-align: left; }
                th { background: #f3f5f8; }
                blockquote { margin: 1.2em 0; padding: .25em 1em; border-left: 4px solid #7b8ba4; color: #4f5a6b; }
                code, pre { font-family: "Cascadia Mono", Consolas, monospace; }
                code { background: #f1f3f5; border-radius: 3px; padding: .12em .3em; }
                pre { padding: 1em; background: #18202d; color: #edf2f7; border-radius: 6px; white-space: pre-wrap; overflow-wrap: anywhere; }
                pre code { padding: 0; background: transparent; }
                .toc { margin: 0 0 2.5em; padding: 1.2em 1.5em; background: #f5f7fa; border-left: 4px solid #315d8a; break-inside: avoid; }
                .toc h2 { margin-top: 0; border: 0; }
                .toc ol { margin: .35em 0; padding-left: 1.35em; }
                hr { border: 0; border-top: 1px solid #d7dce5; margin: 2em 0; }
                @media print {
                  html { background: #fff; }
                  body { max-width: none; padding: 0; }
                  a { color: inherit; }
                  pre, blockquote, table, img { break-inside: avoid; }
                }
              </style>
            </head>
            <body><main class="document">{{body}}</main></body>
            </html>
            """;
    }

    private static string Mm(decimal value) => $"{value.ToString("0.##", CultureInfo.InvariantCulture)}mm";

    private static string EscapeCss(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\"", "\\\"", StringComparison.Ordinal)
        .Replace("\r", string.Empty, StringComparison.Ordinal)
        .Replace("\n", string.Empty, StringComparison.Ordinal);

    private sealed record HeadingInfo(int Level, string Text, string Id);

    private sealed class TocNode(int level, string text, string id)
    {
        public int Level { get; } = level;
        public string Text { get; } = text;
        public string Id { get; } = id;
        public List<TocNode> Children { get; } = [];
    }
}
