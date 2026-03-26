using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace Infrastructure.Services.Internal
{
    internal class OpenAi : AiModel
    {
        private readonly ChatClient _client;

        public OpenAi(ILogger<OpenAi> logger, ChatClient client, int exampleCount) : base(logger, exampleCount)
        {
            _client = client;
        }

        protected async override Task<string> GetAnswerAsync(string prompt)
        {
            ChatCompletion gptAnswer = await _client.CompleteChatAsync(
            [
                new UserChatMessage(prompt),
            ]);

            return gptAnswer.Content[0].Text;
        }
    }
}
