namespace Domain.Interfaces
{
    public interface IExcelService
    {
        void GenerateLexilizeExcel(string directoryPath, (string wordsAndExamples, string meanings)[] source);
    }
}
