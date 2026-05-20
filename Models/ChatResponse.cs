namespace Dociq.Models
{
    public class ChatResponse
    {
        public string Response { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int TokensUsed { get; set; }
    }
}
