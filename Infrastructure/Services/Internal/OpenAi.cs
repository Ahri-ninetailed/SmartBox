using Infrastructure.Interfaces;
using OpenAI.Chat;

namespace Infrastructure.Services.Internal
{
    internal class OpenAi : IAiModel
    {
        private readonly ChatClient _client;

        public OpenAi(ChatClient client)
        {
            _client = client;
        }

        public async Task<string> GetAnswerAsync(string prompt)
        {
            ChatCompletion gptAnswer = await _client.CompleteChatAsync(
            [
                new UserChatMessage(prompt),
            ]);

            return gptAnswer.Content[0].Text;
        }
    }
}
