namespace Domain.Interfaces
{
    public interface IAiService
    {
        Task FillMissingExamplesAsync(IEnumerable<Models.Parser.Word> words);

        Task<List<Models.Parser.Word>> GetMeaningsAndExamplesAsync(string[] words);
    }
}
