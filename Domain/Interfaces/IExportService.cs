using Domain.Models.Parser;

namespace Domain.Interfaces
{
    public interface IExportService
    {
        void GenerateLexilizeExcel(string directoryPath, Word[] source);
        string GetStringForCopyToBuffer(Word domainWord);
    }
}
