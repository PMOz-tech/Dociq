using Dociq.Models;

namespace Dociq.Interfaces;

/// <summary>
/// Orchestrates hybrid retrieval-augmented generation (RAG): embedding → hybrid search → LLM → citations.
/// </summary>
public interface IDocumentQueryService
{
    /// <summary>
    /// Answers a natural-language question by performing hybrid search over previously ingested documents
    /// and generating a grounded, citation-backed response using the configured LLM.
    /// <para>
    /// Internally performs:
    /// <list type="number">
    ///   <item>Embeds the question (dense + sparse vectors).</item>
    ///   <item>Runs a hybrid search in Qdrant using RRF fusion of dense and sparse results.</item>
    ///   <item>Assembles a context prompt from the top-K retrieved chunks.</item>
    ///   <item>Calls the configured LLM with an anti-hallucination system prompt.</item>
    ///   <item>Returns the answer together with source citations.</item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="request">
    /// The query request, including the question and an optional document ID filter.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="DocumentQueryResponse"/> with the answer, citations, model name, and token usage.</returns>
    Task<DocumentQueryResponse> QueryAsync(DocumentQueryRequest request, CancellationToken ct = default);
}
