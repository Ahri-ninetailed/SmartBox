using ClosedXML.Excel;
using Domain.Interfaces;

namespace Infrastructure.Services
{
    public class ExcelService : IExcelService
    {
        public void GenerateLexilizeExcel(string directoryPath, (string wordsAndExamples, string meanings)[] source)
        {
            string fileName = $"Pack{DateTime.Now:ddMMyyHHmm}.xlsx";

            string fullPath = Path.Combine(directoryPath, fileName);

            using (XLWorkbook workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Sheet1");

                for (int i = 0; i < source.Length; i++)
                {
                    worksheet.Cell(i + 1, 1).Value = source[i].meanings;        
                    worksheet.Cell(i + 1, 2).Value = source[i].wordsAndExamples;
                }

                workbook.SaveAs(fullPath);
            }
        }
    }
}
