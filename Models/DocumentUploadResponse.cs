namespace Dociq.Models;

/// <summary>Result returned after a PDF document has been successfully ingested into Qdrant.</summary>
public class DocumentUploadResponse
{
    /// <summary>
    /// Stable GUID assigned to this document. Pass this as <c>DocumentId</c> in query requests
    /// to restrict retrieval to only this document's chunks.
    /// </summary>
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>Original file name of the uploaded PDF.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Number of text chunks stored in Qdrant.</summary>
    public int ChunkCount { get; set; }
}
