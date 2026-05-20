using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Dociq.Implementations;

/// <summary>
/// Extracts text from PDF files using PdfPig, returning one text block per page.
/// </summary>
internal static class PdfTextExtractor
{
    /// <summary>
    /// Opens the PDF from <paramref name="stream"/> and yields the text of each non-empty page.
    /// Words are retrieved in their natural reading order as determined by PdfPig.
    /// </summary>
    /// <param name="stream">A readable stream containing PDF bytes. The stream is read but not disposed.</param>
    /// <returns>
    /// An enumerable of <c>(PageNumber, Text)</c> tuples.
    /// <c>PageNumber</c> is 1-based. Pages with no extractable text are skipped.
    /// </returns>
    public static IEnumerable<(int PageNumber, string Text)> ExtractPages(Stream stream)
    {
        using var pdf = PdfDocument.Open(stream);
        foreach (Page page in pdf.GetPages())
        {
            var text = string.Join(" ", page.GetWords().Select(w => w.Text));
            if (!string.IsNullOrWhiteSpace(text))
                yield return (page.Number, text);
        }
    }
}
