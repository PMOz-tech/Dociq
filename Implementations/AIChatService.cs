using Dociq.Interfaces;
using Dociq.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ChatResponse = Dociq.Models.ChatResponse;

namespace Dociq.Implementations
{
    public class AIChatService : IChatService
    {
        private readonly IChatClient _client;
        private readonly ILogger<AIChatService> _logger;
        private readonly AISettings _settings;

        public AIChatService(
            IChatClient client,
            ILogger<AIChatService> logger,
            IOptions<AISettings> settings)
        {
            _client = client;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<ChatResponse> GetResponseAsync(ChatRequest request, CancellationToken ct = default)
        {
            _logger.LogInformation(
                "Chat request received. MessageLength={Length}",
                request.Message.Length);

            var messages = BuildMessages(request);
            var options = BuildChatOptions();


            var response = await _client.GetResponseAsync(messages, options);

            var tokensUsed = (int)(response.Usage?.TotalTokenCount ?? 0);
            var modelId = response.ModelId ?? _settings.Anthropic.Model;

            _logger.LogInformation(
                "Chat request completed. Model={Model}, TokensUsed={Tokens}",
                modelId, tokensUsed);

            return new ChatResponse
            {
                Response = response.Text,
                Model = modelId,
                TokensUsed = tokensUsed
            };
        }

       
        private List<ChatMessage> BuildMessages(ChatRequest request)
        {
            var messages = new List<ChatMessage>();

            if (!string.IsNullOrWhiteSpace(_settings.SystemPrompt))
                messages.Add(new ChatMessage(ChatRole.System, _settings.SystemPrompt));

            messages.Add(new ChatMessage(ChatRole.User, request.Message));
            return messages;
        }

        private ChatOptions BuildChatOptions() => new()
        {
            ModelId = _settings.Provider == "OpenAI" ? _settings.OpenAI.Model : _settings.Anthropic.Model,
            MaxOutputTokens = _settings.MaxTokens,
            Temperature = _settings.Temperature
        };
    }
}
