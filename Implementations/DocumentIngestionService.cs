#pragma warning disable SKEXP0050  // TextChunker is experimental in SK; acknowledged.

using Dociq.Interfaces;
using Dociq.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Text;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Dociq.Implementations;

/// <summary>
/// Orchestrates PDF ingestion: text extraction → chunking → embedding → Qdrant upsert.
/// </summary>
public sealed class DocumentIngestionService : IDocumentIngestionService
{
    private const int EmbeddingBatchSize = 100;
    private const int QdrantBatchSize = 64;
    private const int ChunkTokenSize = 300;
    private const int ChunkOverlapTokens = 50;

    private readonly QdrantClient _qdrant;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddings;
    private readonly QdrantSettings _qdrantSettings;
    private readonly ILogger<DocumentIngestionService> _logger;

    // Guards idempotent collection creation on cold start.
    private static readonly SemaphoreSlim CollectionInitLock = new(1, 1);
    private static bool _collectionEnsured;

    public DocumentIngestionService(
        QdrantClient qdrant,
        IEmbeddingGenerator<string, Embedding<float>> embeddings,
        IOptions<QdrantSettings> qdrantSettings,
        ILogger<DocumentIngestionService> logger)
    {
        _qdrant = qdrant;
        _embeddings = embeddings;
        _qdrantSettings = qdrantSettings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DocumentUploadResponse> IngestAsync(IFormFile file, CancellationToken ct = default)
    {
        var documentId = Guid.NewGuid();
        var fileName = Path.GetFileName(file.FileName);

        _logger.LogInformation(
            "Ingestion started. DocumentId={DocumentId}, FileName={FileName}, Size={Size}",
            documentId, fileName, file.Length);

        // 1. Extract text pages
        await using var stream = file.OpenReadStream();
        var pages = PdfTextExtractor.ExtractPages(stream).ToList();

        if (pages.Count == 0)
        {
            _logger.LogWarning("PDF {FileName} contained no extractable text.", fileName);
            return new DocumentUploadResponse { DocumentId = documentId.ToString(), FileName = fileName, ChunkCount = 0 };
        }

        // 2. Chunk each page using SK TextChunker
        var allChunks = new List<DocumentChunk>();
        int chunkIndex = 0;
        foreach (var (pageNumber, pageText) in pages)
        {
            var lines = pageText.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
            var chunks = TextChunker.SplitPlainTextParagraphs(lines,
                maxTokensPerParagraph: ChunkTokenSize,
                overlapTokens: ChunkOverlapTokens);

            foreach (var chunkText in chunks)
            {
                allChunks.Add(new DocumentChunk
                {
                    DocumentId = documentId,
                    FileName = fileName,
                    PageNumber = pageNumber,
                    ChunkIndex = chunkIndex++,
                    Text = chunkText
                });
            }
        }

        _logger.LogInformation(
            "Chunking complete. DocumentId={DocumentId}, Pages={Pages}, Chunks={Chunks}",
            documentId, pages.Count, allChunks.Count);

        // 3. Embed in batches
        for (int i = 0; i < allChunks.Count; i += EmbeddingBatchSize)
        {
            var batch = allChunks.Skip(i).Take(EmbeddingBatchSize).ToList();
            var result = await _embeddings.GenerateAsync(
                batch.Select(c => c.Text).ToList(), cancellationToken: ct);

            for (int j = 0; j < batch.Count; j++)
                batch[j].DenseVector = result[j].Vector.ToArray();
        }

        // 4. Compute sparse BM25-style vectors
        foreach (var chunk in allChunks)
            chunk.SparseVector = SparseVectorizer.Vectorize(chunk.Text);

        // 5. Ensure collection exists
        await EnsureCollectionAsync(ct);

        // 6. Upsert to Qdrant in batches
        for (int i = 0; i < allChunks.Count; i += QdrantBatchSize)
        {
            var batch = allChunks.Skip(i).Take(QdrantBatchSize).ToList();
            var points = batch.Select(MapToPoint).ToList();
            await _qdrant.UpsertAsync(_qdrantSettings.CollectionName, points, cancellationToken: ct);
        }

        _logger.LogInformation(
            "Ingestion complete. DocumentId={DocumentId}, Chunks={Chunks}",
            documentId, allChunks.Count);

        return new DocumentUploadResponse
        {
            DocumentId = documentId.ToString(),
            FileName = fileName,
            ChunkCount = allChunks.Count
        };
    }

    private PointStruct MapToPoint(DocumentChunk chunk)
    {
        var sparseValues = chunk.SparseVector!.Values.ToArray();
        var sparseIndices = chunk.SparseVector!.Keys.ToArray();

        return new PointStruct
        {
            Id = chunk.ChunkId,
            Vectors = new Dictionary<string, Vector>
            {
                ["dense"]  = chunk.DenseVector!,
                ["sparse"] = (sparseValues, sparseIndices)
            },
            Payload =
            {
                ["documentId"] = chunk.DocumentId.ToString(),
                ["fileName"]   = chunk.FileName,
                ["pageNumber"] = (long)chunk.PageNumber,
                ["chunkIndex"] = (long)chunk.ChunkIndex,
                ["text"]       = chunk.Text
            }
        };
    }

    private async Task EnsureCollectionAsync(CancellationToken ct)
    {
        if (_collectionEnsured)
            return;

        await CollectionInitLock.WaitAsync(ct);
        try
        {
            if (_collectionEnsured)
                return;

            var exists = await _qdrant.CollectionExistsAsync(_qdrantSettings.CollectionName, ct);
            if (!exists)
            {
                var vectorsMap = new VectorParamsMap();
                vectorsMap.Map.Add("dense", new VectorParams
                {
                    Size = (ulong)_qdrantSettings.DenseVectorDimension,
                    Distance = Distance.Cosine
                });

                SparseVectorConfig sparseConfig = new Dictionary<string, SparseVectorParams>
                {
                    ["sparse"] = new SparseVectorParams
                    {
                        Index = new SparseIndexConfig { OnDisk = false }
                    }
                };

                await _qdrant.CreateCollectionAsync(
                    _qdrantSettings.CollectionName,
                    vectorsMap,
                    sparseVectorsConfig: sparseConfig,
                    cancellationToken: ct);

                _logger.LogInformation(
                    "Created Qdrant collection '{Collection}' with dense({Dim}) + sparse vectors.",
                    _qdrantSettings.CollectionName, _qdrantSettings.DenseVectorDimension);
            }

            _collectionEnsured = true;
        }
        finally
        {
            CollectionInitLock.Release();
        }
    }
}
