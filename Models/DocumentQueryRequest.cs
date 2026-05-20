using System.ComponentModel.DataAnnotations;

namespace Dociq.Models;

/// <summary>Request body for the document query endpoint.</summary>
public class DocumentQueryRequest
{
    /// <summary>Natural-language question to answer using retrieved document context.</summary>
    [Required]
    [MinLength(1)]
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// Optional document ID to restrict retrieval to a single previously uploaded document.
    /// When <see langword="null"/>, all documents in the collection are searched.
    /// </summary>
    public string? DocumentId { get; set; }

    /// <summary>Maximum number of chunks to retrieve from Qdrant (default: 5, max: 20).</summary>
    [Range(1, 20)]
    public int TopK { get; set; } = 5;
}
