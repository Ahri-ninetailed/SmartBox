using ClosedXML.Excel;
using Domain.Interfaces;
using Domain.Models.Parser;
using Shared;

namespace Infrastructure.Services
{
    public class ExportService : IExportService
    {
        private static readonly System.Text.UTF8Encoding Utf8NoBom = new(false);
        public string GenerateLexilizeExcel(string directoryPath, Word[] source)
        {
            string fileName = $"Pack{DateTime.Now:yyMMddHHmmss}.xlsx";

            string fullPath = Path.Combine(directoryPath, fileName);

            using (XLWorkbook workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Sheet1");

                for (int i = 0; i < source.Length; i++)
                {
                    string meanings = MapMeanings(source[i]);
                    string wordAndExamples = MapWordAndExamples(source[i]);

                    worksheet.Cell(i + 1, 1).Value = meanings;
                    worksheet.Cell(i + 1, 2).Value = wordAndExamples;
                }

                workbook.SaveAs(fullPath);
            }

            return fullPath;
        }

        private string MapWordAndExamples(Word word)
        {
            string preposition = (word.Type == PartOfSpeech.Verb || word.Type == PartOfSpeech.PhrasalVerb) ?
                "to " : "";

            int countOfMeanings = word.Definitions.Count;
            string countOfMeaningsStr = countOfMeanings > 1 ? $" ({countOfMeanings})" : "";

            string[] examples = word.Definitions
                .SelectMany(def => def.Examples.Select(ex => ex.Value))
                .Select(el =>
                    el.ToUpperFirstChar()
                      .Replace('(', '[')
                      .Replace(')', ']'))
                .ToArray();
            string examplesStr = examples.Length > 0 ? $" ({string.Join(". ", examples)})" : "";

            return $"{preposition}{string.Join(", ", new[] { word.Value }.Concat(word.OtherForms))}{countOfMeaningsStr}{examplesStr}";
        }

        private string MapMeanings(Word word)
        {
            return string.Join(". ", word.Definitions.Select(def => def.Value.ToUpperFirstChar()));
        }

        public string GenerateAnkiTsv(string directoryPath, Word[] source)
        {
            Directory.CreateDirectory(directoryPath);

            string fileName = $"Anki{DateTime.Now:yyMMddHHmmss}.txt";
            string fullPath = Path.Combine(directoryPath, fileName);

            string content = string.Join("\n", source.Select(MapAnkiRow));
            File.WriteAllText(fullPath, content, Utf8NoBom);

            return fullPath;
        }

        private static string MapAnkiRow(Word word)
        {
            string preposition = word.Type == PartOfSpeech.Verb || word.Type == PartOfSpeech.PhrasalVerb
                ? "to " : "";
            string wordField = preposition + word.Value;

            int count = word.Definitions.Count;

            string meaningsField = string.Join("\n", word.Definitions.Select((def, i) => $"{i + 1}. {def.Value}"));

            IEnumerable<string> exampleGroups = word.Definitions
                .Select((def, i) => (Index: i + 1, def.Examples))
                .Where(g => g.Examples.Count > 0)
                .Select(g =>
                {
                    string examples = string.Join("\n", g.Examples.Select(ex => ex.Value.ToUpperFirstChar()));
                    return $"({g.Index}) \n{examples}";
                });
            string examplesField = string.Join("\n\n", exampleGroups);

            return $"{wordField}\t{count}\t{QuoteTsvField(meaningsField)}\t{QuoteTsvField(examplesField)}";
        }

        private static string QuoteTsvField(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        public string GetStringForCopyToBuffer(Word word)
        {
            return $"{MapWordAndExamples(word)} -> {MapMeanings(word)}";
        }
    }
}
