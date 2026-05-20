namespace Dociq.Models;

public class AISettings
{
    /// <summary>Chat LLM provider. Valid values: "Anthropic", "OpenAI", "Ollama".</summary>
    public string Provider { get; set; } = "Anthropic";

    /// <summary>Embedding provider. Valid values: "OpenAI", "Ollama".</summary>
    public string EmbeddingProvider { get; set; } = "Ollama";

    public string SystemPrompt { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 1024;
    public float Temperature { get; set; } = 1.0f;

    public AnthropicSettings Anthropic { get; set; } = new();
    public OpenAISettings OpenAI { get; set; } = new();
    public OllamaSettings Ollama { get; set; } = new();
}

public class AnthropicSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-sonnet-4-20250514";
}

public class OpenAISettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o";
}

public class OllamaSettings
{
    /// <summary>Base URL of the Ollama server (default: http://localhost:11434).</summary>
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>Model used for chat when Provider = "Ollama" (e.g. llama3.2, mistral).</summary>
    public string ChatModel { get; set; } = "llama3.2";

    /// <summary>
    /// Model used for embeddings when EmbeddingProvider = "Ollama".
    /// nomic-embed-text → 768 dimensions. mxbai-embed-large → 1024 dimensions.
    /// Make sure QdrantSettings:DenseVectorDimension matches the model you choose.
    /// </summary>
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
}
