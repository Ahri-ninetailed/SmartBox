namespace Application.Interfaces
{
    public interface IParserService
    {
        Task<List<Models.Parser.Word>> ExtractWordDataAsync(string[] entries);

        void ExportWords(string directoryPath, Models.Parser.Word[] source);
    }
}
