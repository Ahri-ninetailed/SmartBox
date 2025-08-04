namespace Application.Interfaces
{
    public interface IParserService
    {
        Task<List<Models.Parser.Word>> ExtractWordDataAsync(string entry);
    }
}
