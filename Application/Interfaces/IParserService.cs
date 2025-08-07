namespace Application.Interfaces
{
    public interface IParserService
    {
        Task<List<Models.Parser.Word>> GetDefinitionsAndExamples(string[] entries);

        Task<Models.Parser.Word> GetWordFromDb(int wordId);

        void ExportWords(string directoryPath, Models.Parser.Word[] source);
    }
}
