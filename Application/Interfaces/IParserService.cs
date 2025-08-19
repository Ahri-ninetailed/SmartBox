namespace Application.Interfaces
{
    public interface IParserService
    {
        Task<List<Models.Parser.Word>> GetDefinitionsAndExamples(string[] entries, bool isAiEnabled);

        Task<Models.Parser.Word> GetWordFromDb(int wordId);

        void ExportWords(string directoryPath, Models.Parser.Word[] source);

        Task SyncDefinitionsAndExamplesAsync(int wordId, IEnumerable<Models.Parser.Definition> newDefinitions);
    }
}
