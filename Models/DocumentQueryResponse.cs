namespace Dociq.Models;

/// <summary>Response returned by the document query endpoint.</summary>
public class DocumentQueryResponse
{
    /// <summary>Generated answer grounded exclusively in the retrieved document context.</summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// Source citations that the answer was derived from, ordered by descending relevance score.
    /// Each citation corresponds to a chunk retrieved from Qdrant.
    /// </summary>
    public List<CitationDto> Citations { get; set; } = [];

    /// <summary>LLM model identifier used to generate the answer.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Total tokens consumed by the LLM generation call.</summary>
    public int TokensUsed { get; set; }
}
