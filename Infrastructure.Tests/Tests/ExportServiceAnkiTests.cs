using Domain.Models.Parser;
using Infrastructure.Services;
using System.Text;

namespace Infrastructure.Tests.Tests
{
    public class ExportServiceAnkiTests : IDisposable
    {
        private readonly ExportService _service = new();
        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        [Fact]
        public void GenerateAnkiTsv_Verb_AddsToPrefix()
        {
            var words = new[] { MakeWord("run", PartOfSpeech.Verb, Def("move quickly", "She runs.")) };

            string content = Generate(words);

            Assert.StartsWith("to run\t", content);
        }

        [Fact]
        public void GenerateAnkiTsv_PhrasalVerb_AddsToPrefix()
        {
            var words = new[] { MakeWord("give up", PartOfSpeech.PhrasalVerb, Def("stop trying", "He gave up.")) };

            string content = Generate(words);

            Assert.StartsWith("to give up\t", content);
        }

        [Fact]
        public void GenerateAnkiTsv_Noun_NoPrefix()
        {
            var words = new[] { MakeWord("book", PartOfSpeech.Noun, Def("a written work", "I read a book.")) };

            string content = Generate(words);

            Assert.StartsWith("book\t", content);
        }

        [Fact]
        public void GenerateAnkiTsv_CountField_MatchesDefinitionCount()
        {
            var words = new[]
            {
                MakeWord("run", PartOfSpeech.Verb,
                    Def("move quickly", "She runs."),
                    Def("operate", "The engine runs."),
                    Def("flow", "Water runs."))
            };

            string[] fields = SplitFields(Generate(words));

            Assert.Equal("3", fields[1]);
        }

        [Fact]
        public void GenerateAnkiTsv_MeaningsField_NumberedAndNewlineSeparated()
        {
            var words = new[]
            {
                MakeWord("run", PartOfSpeech.Verb,
                    Def("move quickly"),
                    Def("operate"))
            };

            string[] fields = SplitFields(Generate(words));

            Assert.Equal("\"1. move quickly\n2. operate\"", fields[2]);
        }

        [Fact]
        public void GenerateAnkiTsv_ExamplesField_GroupedByDefinition()
        {
            var words = new[]
            {
                MakeWord("run", PartOfSpeech.Verb,
                    Def("move quickly", "She runs.", "He ran."),
                    Def("operate", "The engine runs well."))
            };

            string[] fields = SplitFields(Generate(words));

            Assert.Equal("\"(1) \nShe runs.\nHe ran.\n\n(2) \nThe engine runs well.\"", fields[3]);
        }

        [Fact]
        public void GenerateAnkiTsv_DefinitionWithNoExamples_GroupSkipped()
        {
            var words = new[]
            {
                MakeWord("run", PartOfSpeech.Verb,
                    Def("move quickly"),
                    Def("operate", "The engine runs."))
            };

            string[] fields = SplitFields(Generate(words));

            Assert.Equal("\"(2) \nThe engine runs.\"", fields[3]);
        }

        [Fact]
        public void GenerateAnkiTsv_ExamplesCapitalized()
        {
            var words = new[] { MakeWord("run", PartOfSpeech.Verb, Def("move quickly", "she runs every day.")) };

            string[] fields = SplitFields(Generate(words));

            Assert.Contains("She runs every day.", fields[3]);
        }

        [Fact]
        public void GenerateAnkiTsv_QuotesInField_Doubled()
        {
            var words = new[] { MakeWord("book", PartOfSpeech.Noun, Def("a so-called \"classic\" work")) };

            string[] fields = SplitFields(Generate(words));

            Assert.Contains("\"\"classic\"\"", fields[2]);
        }

        [Fact]
        public void GenerateAnkiTsv_EmptySource_EmptyFile()
        {
            string content = Generate([]);

            Assert.Equal(string.Empty, content);
        }

        [Fact]
        public void GenerateAnkiTsv_MultipleWords_RowsSeparatedByNewline()
        {
            var words = new[]
            {
                MakeWord("book", PartOfSpeech.Noun, Def("a written work", "I read a book.")),
                MakeWord("run", PartOfSpeech.Verb, Def("move quickly", "She runs."))
            };

            string content = Generate(words);

            Assert.StartsWith("book\t", content);
            Assert.Contains("\nto run\t", content);
        }

        [Fact]
        public void GenerateAnkiTsv_FileHasNoBom()
        {
            var words = new[] { MakeWord("book", PartOfSpeech.Noun, Def("a written work", "I read a book.")) };

            string path = _service.GenerateAnkiTsv(_tempDir, words);
            byte[] bytes = File.ReadAllBytes(path);

            Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
        }

        [Fact]
        public void GenerateAnkiTsv_ReturnsExistingFilePath()
        {
            var words = new[] { MakeWord("book", PartOfSpeech.Noun, Def("a written work", "I read a book.")) };

            string path = _service.GenerateAnkiTsv(_tempDir, words);

            Assert.True(File.Exists(path));
            Assert.StartsWith("Anki", Path.GetFileNameWithoutExtension(path));
        }

        [Fact]
        public void GetStringForCopyToBuffer_ReturnsSameRowAsAnkiExport()
        {
            Word word = MakeWord("run", PartOfSpeech.Verb,
                Def("move quickly", "she runs."),
                Def("operate", "the engine runs."));

            string copied = _service.GetStringForCopyToBuffer(word);

            Assert.Equal(Generate([word]), copied);
        }

        [Fact]
        public void GetStringForCopyToBuffer_HasFourAnkiFields()
        {
            Word word = MakeWord("book", PartOfSpeech.Noun, Def("a written work", "I read a book."));

            string[] fields = SplitFields(_service.GetStringForCopyToBuffer(word));

            Assert.Equal(4, fields.Length);
            Assert.Equal("book", fields[0]);
        }

        // helpers

        private string Generate(Word[] words)
        {
            string path = _service.GenerateAnkiTsv(_tempDir, words);
            return File.ReadAllText(path, Encoding.UTF8);
        }

        private static string[] SplitFields(string row) => row.Split('\t');

        private static Word MakeWord(string value, PartOfSpeech type, params Definition[] definitions) =>
            new() { Value = value, Type = type, Definitions = definitions.ToList() };

        private static Definition Def(string value, params string[] examples) =>
            new()
            {
                Value = value,
                Examples = examples.Select(e => new Example { Value = e }).ToList()
            };
    }
}
