using Dociq.Implementations;
using Dociq.Interfaces;
using Dociq.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OpenAI;
using Qdrant.Client;

namespace Dociq.DependencyInjection;

/// <summary>Extension methods that register the RAG pipeline services into the DI container.</summary>
public static class RagExtensions
{
    /// <summary>
    /// Registers:
    /// <list type="bullet">
    ///   <item><see cref="QdrantSettings"/> bound from the "QdrantSettings" configuration section.</item>
    ///   <item><see cref="QdrantClient"/> as a singleton gRPC client.</item>
    ///   <item>
    ///     <see cref="IEmbeddingGenerator{String,Embedding}"/> — provider selected by
    ///     <c>AISettings.EmbeddingProvider</c>:
    ///     <list type="bullet">
    ///       <item><c>"Ollama"</c> — local Ollama server, no API key required.</item>
    ///       <item><c>"OpenAI"</c> — OpenAI API, requires <c>AISettings.OpenAI.ApiKey</c>.</item>
    ///     </list>
    ///   </item>
    ///   <item><see cref="IDocumentIngestionService"/> and <see cref="IDocumentQueryService"/> as scoped services.</item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddRagServices(this IServiceCollection services)
    {
        services.AddOptions<QdrantSettings>()
            .BindConfiguration("QdrantSettings")
            .ValidateOnStart();

        // Singleton Qdrant gRPC client — thread-safe and inexpensive to share.
        services.AddSingleton<QdrantClient>(sp =>
        {
            var s = sp.GetRequiredService<IOptions<QdrantSettings>>().Value;
            return new QdrantClient(host: s.Host, port: s.Port, apiKey: s.ApiKey);
        });

        // Embedding generator — provider selected by AISettings.EmbeddingProvider.
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            var ai = sp.GetRequiredService<IOptions<AISettings>>().Value;

            return ai.EmbeddingProvider switch
            {
                "Ollama" => new OllamaApiClient(new Uri(ai.Ollama.BaseUrl), ai.Ollama.EmbeddingModel),

                "OpenAI" => new OpenAIClient(ai.OpenAI.ApiKey)
                                .GetEmbeddingClient("text-embedding-3-small")
                                .AsIEmbeddingGenerator(),

                _ => throw new InvalidOperationException(
                    $"Unknown embedding provider: '{ai.EmbeddingProvider}'. Valid values: Ollama, OpenAI")
            };
        });

        services.AddScoped<IDocumentIngestionService, DocumentIngestionService>();
        services.AddScoped<IDocumentQueryService, DocumentQueryService>();

        return services;
    }
}
