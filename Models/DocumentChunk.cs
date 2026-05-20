namespace Dociq.Models;

/// <summary>
/// Internal representation of a single text chunk produced during PDF ingestion.
/// Not serialized to HTTP responses — used only within the ingestion pipeline.
/// </summary>
internal sealed class DocumentChunk
{
    public Guid ChunkId { get; init; } = Guid.NewGuid();
    public Guid DocumentId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public int PageNumber { get; init; }
    public int ChunkIndex { get; init; }
    public string Text { get; init; } = string.Empty;

    /// <summary>Dense embedding vector, populated after the embedding step.</summary>
    public float[]? DenseVector { get; set; }

    /// <summary>
    /// Sparse BM25-style vector (termHash → normalizedTF), populated after the sparse vectorization step.
    /// </summary>
    public Dictionary<uint, float>? SparseVector { get; set; }
}
