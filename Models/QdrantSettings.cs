namespace Dociq.Models;

/// <summary>Qdrant connection and collection settings, bound from the "QdrantSettings" config section.</summary>
public class QdrantSettings
{
    /// <summary>Hostname of the Qdrant instance (default: localhost).</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>gRPC port of the Qdrant instance (default: 6334).</summary>
    public int Port { get; set; } = 6334;

    /// <summary>Optional API key for Qdrant Cloud or secured self-hosted deployments.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Name of the collection that stores document chunks (default: documents).</summary>
    public string CollectionName { get; set; } = "documents";

    /// <summary>
    /// Dimension of the dense embedding vectors.
    /// Must match the embedding model — text-embedding-3-small produces 1536-dimensional vectors.
    /// </summary>
    public int DenseVectorDimension { get; set; } = 1536;
}
