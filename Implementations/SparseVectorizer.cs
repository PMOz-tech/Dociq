namespace Dociq.Implementations;

/// <summary>
/// Produces sparse BM25-style vectors from raw text for use in Qdrant hybrid search.
/// <para>
/// Each unique token in the text is mapped to a <c>uint</c> index via a stable FNV-1a 32-bit hash,
/// and the associated value is the token's normalized term frequency (TF / totalTokens).
/// Both the upload-time and query-time vectorization run inside the same process, so hash stability
/// across process restarts is not required — a fast, collision-resistant hash is sufficient.
/// </para>
/// </summary>
internal static class SparseVectorizer
{
    private static readonly char[] Separators =
        [' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']', '{', '}', '-', '/'];

    /// <summary>
    /// Tokenizes <paramref name="text"/> and returns a sparse vector as a dictionary
    /// of <c>termHash → normalizedTF</c> pairs.
    /// Returns an empty dictionary for blank or whitespace-only input.
    /// </summary>
    public static Dictionary<uint, float> Vectorize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var tokens = text
            .ToLowerInvariant()
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length == 0)
            return [];

        var termCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var token in tokens)
            termCounts[token] = termCounts.TryGetValue(token, out var c) ? c + 1 : 1;

        float total = tokens.Length;
        var result = new Dictionary<uint, float>(termCounts.Count);
        foreach (var (term, count) in termCounts)
        {
            var idx = Fnv1a32(term);
            // On the rare collision, the last writer wins — acceptable precision loss.
            result[idx] = count / total;
        }
        return result;
    }

    /// <summary>FNV-1a 32-bit hash — deterministic, fast, and collision-resistant for short tokens.</summary>
    private static uint Fnv1a32(string text)
    {
        const uint FnvOffset = 2166136261u;
        const uint FnvPrime  = 16777619u;

        uint hash = FnvOffset;
        foreach (char ch in text)
        {
            hash ^= ch;
            hash *= FnvPrime;
        }
        return hash;
    }
}
