using Dociq.Models;
using ChatResponse = Dociq.Models.ChatResponse;

namespace Dociq.Interfaces
{
    public interface IChatService
    {
        Task<ChatResponse> GetResponseAsync(ChatRequest request, CancellationToken ct = default);

    }
}
