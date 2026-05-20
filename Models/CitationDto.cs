namespace Dociq.Models;

/// <summary>A single source chunk retrieved from Qdrant that supports the generated answer.</summary>
public class CitationDto
{
    /// <summary>Qdrant point UUID of this chunk.</summary>
    public string ChunkId { get; set; } = string.Empty;

    /// <summary>Stable document GUID assigned at upload time.</summary>
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>Original file name of the uploaded PDF.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>1-based page number in the source PDF (0 when unknown).</summary>
    public int PageNumber { get; set; }

    /// <summary>Verbatim text excerpt from the retrieved chunk.</summary>
    public string Excerpt { get; set; } = string.Empty;

    /// <summary>Hybrid search relevance score (RRF-fused) returned by Qdrant.</summary>
    public float Score { get; set; }
}
