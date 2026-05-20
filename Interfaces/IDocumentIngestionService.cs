using Dociq.Models;

namespace Dociq.Interfaces;

/// <summary>
/// Orchestrates PDF ingestion: text extraction → chunking → embedding → Qdrant storage.
/// </summary>
public interface IDocumentIngestionService
{
    /// <summary>
    /// Ingests a PDF file into the Qdrant vector store.
    /// <para>
    /// Internally performs:
    /// <list type="number">
    ///   <item>Text extraction per page using PdfPig.</item>
    ///   <item>Chunking via Semantic Kernel <c>TextChunker</c> (300 tokens, 50-token overlap).</item>
    ///   <item>Dense embedding via OpenAI <c>text-embedding-3-small</c>.</item>
    ///   <item>Sparse BM25-style vectorization (TF tokenization).</item>
    ///   <item>Batch upsert into Qdrant with named vectors "dense" and "sparse".</item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="file">The uploaded PDF file from a multipart/form-data request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="DocumentUploadResponse"/> containing the assigned document ID and chunk count.</returns>
    Task<DocumentUploadResponse> IngestAsync(IFormFile file, CancellationToken ct = default);
}
