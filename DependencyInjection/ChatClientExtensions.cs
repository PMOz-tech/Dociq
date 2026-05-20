using Anthropic;
using Dociq.Implementations;
using Dociq.Interfaces;
using Dociq.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OpenAI;

namespace Dociq.DependencyInjection;

public static class ChatClientExtensions
{
    /// <summary>
    /// Registers <see cref="IChatClient"/> and <see cref="IChatService"/>.
    /// The chat provider is selected by <c>AISettings.Provider</c>:
    /// <list type="bullet">
    ///   <item><c>"Anthropic"</c> — Anthropic Claude, requires <c>AISettings.Anthropic.ApiKey</c>.</item>
    ///   <item><c>"OpenAI"</c>   — OpenAI GPT, requires <c>AISettings.OpenAI.ApiKey</c>.</item>
    ///   <item><c>"Ollama"</c>   — local Ollama server, no API key required.</item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddAIChatServices(this IServiceCollection services)
    {
        services.AddOptions<AISettings>()
            .BindConfiguration("AISettings")
            .ValidateOnStart();

        services.AddSingleton<IChatClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<AISettings>>().Value;

            var inner = settings.Provider switch
            {
                "OpenAI" => new OpenAIClient(settings.OpenAI.ApiKey)
                                .GetChatClient(settings.OpenAI.Model)
                                .AsIChatClient(),

                "Anthropic" => new AnthropicClient { ApiKey = settings.Anthropic.ApiKey }
                                .AsIChatClient(settings.Anthropic.Model),

                "Ollama" => (IChatClient)new OllamaApiClient(
                                new Uri(settings.Ollama.BaseUrl), settings.Ollama.ChatModel),

                _ => throw new InvalidOperationException(
                    $"Unknown AI provider: '{settings.Provider}'. Valid values: Anthropic, OpenAI, Ollama")
            };

            return new ChatClientBuilder(inner)
                .UseFunctionInvocation()
                .Build();
        });

        services.AddScoped<IChatService, AIChatService>();

        return services;
    }
}
