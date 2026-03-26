using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace Infrastructure.Services.Internal
{
    internal class OpenAi : IAiModel
    {
        private readonly ILogger<OpenAi> _logger;
        private readonly ChatClient _client;
        private readonly int? _maxOutputTokens;

        public OpenAi(ILogger<OpenAi> logger, ChatClient client, int? maxOutputTokens)
        {
            _logger = logger;
            _client = client;
            _maxOutputTokens = maxOutputTokens;
        }

        public async Task<string> GetAnswerAsync(string prompt, string jsonSchemaName, string jsonSchema)
        {
            _logger.LogInformation("Отправка промпта:\n{Prompt}", prompt);

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = _maxOutputTokens
            };

            options.ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: jsonSchemaName,
                    jsonSchema: BinaryData.FromString(jsonSchema),
                    jsonSchemaIsStrict: true);

            ChatCompletion gptAnswer = await _client.CompleteChatAsync(
            [
                new UserChatMessage(prompt),
            ], options);

            string answer = gptAnswer.Content[0].Text;

            _logger.LogInformation("Ответ от модели получен:\n{Answer}", answer);

            return answer;
        }
    }
}
