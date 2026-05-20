using Dociq.Interfaces;
using Dociq.Models;
using Microsoft.AspNetCore.Mvc;

namespace Dociq.Controllers;

/// <summary>
/// Provides two endpoints for the Dociq RAG pipeline:
/// <list type="bullet">
///   <item>
///     <term>POST /api/documents/upload</term>
///     <description>
///       Ingest a PDF: extracts text, chunks with Semantic Kernel, embeds with OpenAI
///       <c>text-embedding-3-small</c>, and stores dense + sparse vectors in Qdrant.
///     </description>
///   </item>
///   <item>
///     <term>POST /api/documents/query</term>
///     <description>
///       Answer a natural-language question via hybrid retrieval-augmented generation.
///       Dense and sparse vectors are fused with RRF before passing context to the LLM.
///       The LLM is constrained by an anti-hallucination system prompt and citations are
///       returned alongside the generated answer.
///     </description>
///   </item>
/// </list>
/// </summary>
[ApiController]
[Route("api/documents")]
[Produces("application/json")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentIngestionService _ingestion;
    private readonly IDocumentQueryService _query;
    private readonly ILogger<DocumentsController> _logger;

    /// <summary>Initializes the controller with RAG pipeline services.</summary>
    public DocumentsController(
        IDocumentIngestionService ingestion,
        IDocumentQueryService query,
        ILogger<DocumentsController> logger)
    {
        _ingestion = ingestion;
        _query     = query;
        _logger    = logger;
    }

    /// <summary>
    /// Upload a PDF and ingest it into the Qdrant vector store.
    /// </summary>
    /// <remarks>
    /// The file is processed in memory:
    ///
    /// 1. Text is extracted page-by-page using PdfPig.
    /// 2. Each page is chunked with Semantic Kernel `TextChunker` (300-token paragraphs, 50-token overlap).
    /// 3. Chunks are embedded with OpenAI `text-embedding-3-small` (1536 dimensions).
    /// 4. A BM25-style sparse vector is computed for each chunk via TF tokenization.
    /// 5. Both vectors are upserted into Qdrant under the named vectors `"dense"` and `"sparse"`.
    ///
    /// The returned `documentId` can be passed to the `/query` endpoint to restrict search to this document.
    /// </remarks>
    /// <param name="file">A PDF file (Content-Type: application/pdf). Send as multipart/form-data with key `file`.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The assigned document ID, original file name, and number of stored chunks.</returns>
    /// <response code="200">Document ingested successfully.</response>
    /// <response code="400">File is missing, empty, or not a PDF.</response>
    /// <response code="500">Ingestion failed due to a server-side error.</response>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<DocumentUploadResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("A non-empty PDF file is required.");

        if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only PDF files (Content-Type: application/pdf) are accepted.");

        _logger.LogInformation(
            "Upload request received. FileName={Name}, Size={Size}B",
            file.FileName, file.Length);

        var result = await _ingestion.IngestAsync(file, ct);
        return Ok(result);
    }

    /// <summary>
    /// Ask a natural-language question and receive a grounded answer with source citations.
    /// </summary>
    /// <remarks>
    /// Retrieval uses **hybrid search** — Qdrant's RRF (Reciprocal Rank Fusion) merges a dense
    /// ANN pass and a sparse BM25-style pass, then returns the top-K chunks by fused score.
    ///
    /// The LLM (configured via `AISettings.Provider`) is called with:
    /// - A strict **anti-hallucination system prompt** that forbids answering outside the context.
    /// - The retrieved chunks injected as `[Source N]` blocks in the user message.
    ///
    /// If no relevant chunks are found, the endpoint returns a canned "I don't know" response
    /// without calling the LLM at all.
    ///
    /// **Request body example:**
    /// ```json
    /// {
    ///   "question": "What are the key findings of the report?",
    ///   "documentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///   "topK": 5
    /// }
    /// ```
    ///
    /// Omit `documentId` to search across all ingested documents.
    /// </remarks>
    /// <param name="request">The query containing the question, an optional document filter, and the desired chunk count.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The generated answer, source citations with page references, model name, and token usage.</returns>
    /// <response code="200">Query answered successfully.</response>
    /// <response code="400">Question is empty or the request is malformed.</response>
    /// <response code="500">Query failed due to a server-side error.</response>
    [HttpPost("query")]
    [ProducesResponseType<DocumentQueryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Query([FromBody] DocumentQueryRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question must not be empty.");

        _logger.LogInformation(
            "Query request received. Question={Question}, DocumentId={DocId}, TopK={TopK}",
            request.Question, request.DocumentId ?? "(all)", request.TopK);

        var result = await _query.QueryAsync(request, ct);
        return Ok(result);
    }
}
