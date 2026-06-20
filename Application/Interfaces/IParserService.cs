using Application.Enums;
using Domain.Models.Parser;

namespace Application.Interfaces
{
    public interface IParserService
    {
        Task<List<Word>> GetDefinitionsAndExamples(string[] entries, AiMode aiMode);

        Task<Domain.Models.Parser.Word> GetWordFromDb(int wordId);

        string ExportLexilizeWords(string directoryPath, Domain.Models.Parser.Word[] source);
        string ExportAnkiWords(string directoryPath, Domain.Models.Parser.Word[] source);

        Task SyncDefinitionsAndExamplesAsync(int wordId, IEnumerable<Domain.Models.Parser.Definition> newDefinitions);
        string GetStringForCopyToBuffer(Domain.Models.Parser.Word word);
    }
}
