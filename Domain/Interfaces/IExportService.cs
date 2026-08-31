using Domain.Models.Parser;

namespace Domain.Interfaces
{
    public interface IExportService
    {
        string GenerateAnkiTsv(string directoryPath, Word[] source);
        string GetStringForCopyToBuffer(Word domainWord);
    }
}
