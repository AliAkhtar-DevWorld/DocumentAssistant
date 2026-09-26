namespace DocumentAssistant.Models
{
    public class ChatViewModel
    {
        public string Question { get; set; } = "";
        public string? Answer { get; set; }
        public string? Error { get; set; }
    }
}
