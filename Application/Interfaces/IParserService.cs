namespace Application.Interfaces
{
    public interface IParserService
    {
        Task<List<Domain.Models.Parser.Word>> GetDefinitionsAndExamples(string[] entries, bool isAiEnabled, bool isAiLocalOnly);

        Task<Domain.Models.Parser.Word> GetWordFromDb(int wordId);

        void ExportWords(string directoryPath, Domain.Models.Parser.Word[] source);

        Task SyncDefinitionsAndExamplesAsync(int wordId, IEnumerable<Domain.Models.Parser.Definition> newDefinitions);
        string GetStringForCopyToBuffer(Domain.Models.Parser.Word word);
    }
}
