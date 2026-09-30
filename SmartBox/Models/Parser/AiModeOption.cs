using Application.Enums;

namespace SmartBox.Models.Parser
{
    public class AiModeOption
    {
        public required AiMode Mode { get; init; }

        public required string Title { get; init; }
    }
}
