using Domain.Models.Parser;

namespace Domain.Interfaces
{
    public interface IExcelService
    {
        void GenerateLexilizeExcel(string directoryPath, Word[] source);
    }
}
