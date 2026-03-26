namespace Infrastructure.Interfaces
{
    public interface IAiModel
    {
        Task<string> GetAnswerAsync(string prompt, string jsonSchemaName, string jsonSchema);
    }
}
