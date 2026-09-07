using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlToOpenXml;
using System.Text.RegularExpressions;

namespace WinMdConverter.Core;

public sealed class DocxConverter
{
    private const decimal TwipsPerMillimeter = 1440m / 25.4m;

    public async Task ConvertAsync(
        string html,
        string outputPath,
        ConversionOptions options,
        CancellationToken cancellationToken = default)
    {
        var temporaryPath = Path.Combine(
            Path.GetDirectoryName(outputPath)!,
            $".{Path.GetFileName(outputPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (var document = WordprocessingDocument.Create(temporaryPath, WordprocessingDocumentType.Document))
            {
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document(new Body());
                AddDocumentDefaults(mainPart, options.FontFamily);

                var converter = new HtmlConverter(mainPart)
                {
                    SupportsAnchorLinks = true,
                    SupportsHeadingNumbering = false,
                    ImageProcessing = ImageProcessingMode.EmbedDataUriOnly
                };

                await converter.ParseBody(RemoveUnsupportedImages(html), cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                ApplyPageSettings(mainPart.Document.Body!, options);
                mainPart.Document.Save();
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, outputPath, options.Overwrite);
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    private static void AddDocumentDefaults(MainDocumentPart mainPart, string? selectedFont)
    {
        var font = string.IsNullOrWhiteSpace(selectedFont) ? "Microsoft JhengHei" : selectedFont;
        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = new Styles(
            new DocDefaults(
                new RunPropertiesDefault(
                    new RunPropertiesBaseStyle(
                        new RunFonts
                        {
                            Ascii = font,
                            HighAnsi = font,
                            EastAsia = font,
                            ComplexScript = font
                        })),
                new ParagraphPropertiesDefault()));
        stylesPart.Styles.Save();
    }

    private static void ApplyPageSettings(Body body, ConversionOptions options)
    {
        body.Elements<SectionProperties>().ToList().ForEach(properties => properties.Remove());

        var landscape = options.Orientation == PageOrientation.Landscape;
        var pageSize = new PageSize
        {
            Width = landscape ? 16838U : 11906U,
            Height = landscape ? 11906U : 16838U,
            Orient = landscape ? PageOrientationValues.Landscape : PageOrientationValues.Portrait
        };
        var margins = ConversionOptionsValidator.ResolveMargins(options);
        var pageMargin = new PageMargin
        {
            Top = ToTwips(margins.Top),
            Right = (uint)ToTwips(margins.Right),
            Bottom = ToTwips(margins.Bottom),
            Left = (uint)ToTwips(margins.Left),
            Header = 0U,
            Footer = 0U,
            Gutter = 0U
        };

        body.Append(new SectionProperties(pageSize, pageMargin));
    }

    private static int ToTwips(decimal millimeters) =>
        (int)Math.Round(millimeters * TwipsPerMillimeter, MidpointRounding.AwayFromZero);

    private static string RemoveUnsupportedImages(string html) => Regex.Replace(
        html,
        "<img\\b[^>]*\\bsrc\\s*=\\s*([\"'])data:image/webp;.*?\\1[^>]*>",
        string.Empty,
        RegexOptions.IgnoreCase | RegexOptions.Singleline,
        TimeSpan.FromSeconds(1));

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
