using Application.Enums;
using Application.Services;
using Application.Settings;
using Domain.Interfaces;
using Domain.Models.Parser;

namespace Application.Tests.Tests
{
    public class ParserServiceTests
    {
        private readonly FakeWordSource _wordSource = new();
        private readonly FakeAiService _aiService = new();
        private readonly ParserService _service;

        public ParserServiceTests()
        {
            // Экспорт, репозиторий и мапперы в сценарии получения определений не участвуют
            _service = new ParserService(_wordSource, null!, _aiService, null!, null!, null!,
                                         new AiSettings(fillMissingExamplesIfBelow: 2, examplesPerMeaningForFilling: 3, examplesPerMeaningForNewWords: 4));
        }

        [Fact]
        public async Task GetDefinitionsAndExamples_SomeMeaningsLackExamples_AsksAiOnlyForThemAndAddsExamples()
        {
            _wordSource.Words = [MakeWord("reek", ("to smell bad", 0), ("to be full of something", 2))];
            _aiService.Examples = new() { [new WordMeaning("reek", "to smell bad")] = ["The garbage reeked", "His breath reeked"] };

            List<Word> words = await _service.GetDefinitionsAndExamples(["reek"], AiMode.InputOnly);

            Assert.Equal([new WordMeaning("reek", "to smell bad")], _aiService.RequestedMeanings);
            Assert.Equal(3, _aiService.RequestedExamplesPerMeaning);
            Assert.Equal(["The garbage reeked", "His breath reeked"], words[0].Definitions[0].Examples.Select(ex => ex.Value));
            Assert.Equal(2, words[0].Definitions[1].Examples.Count);
        }

        [Fact]
        public async Task GetDefinitionsAndExamples_AllMeaningsHaveEnoughExamples_DoesNotAskAiForExamples()
        {
            _wordSource.Words = [MakeWord("reek", ("to smell bad", 2))];

            await _service.GetDefinitionsAndExamples(["reek"], AiMode.InputOnly);

            Assert.Null(_aiService.RequestedMeanings);
        }

        [Fact]
        public async Task GetDefinitionsAndExamples_AiEnabled_AsksAiForNewWordsWithConfiguredExampleCount()
        {
            await _service.GetDefinitionsAndExamples(["Reek "], AiMode.InputOnly);

            Assert.Equal(["reek"], _aiService.RequestedWords);
            Assert.Equal(4, _aiService.RequestedExamplesPerNewWordMeaning);
        }

        [Fact]
        public async Task GetDefinitionsAndExamples_FullMode_AsksAiForInputAndFoundWordsWithVariants()
        {
            _wordSource.Words = [new Word { Value = "under way", OtherForms = ["underway"], PluralForms = ["under ways"] }];

            await _service.GetDefinitionsAndExamples(["Underway"], AiMode.Full);

            Assert.Equal(["underway", "under way"], _aiService.RequestedWords);
        }

        [Fact]
        public async Task GetDefinitionsAndExamples_InputOnlyMode_AsksAiOnlyForInput()
        {
            _wordSource.Words = [new Word { Value = "under way", OtherForms = ["underway"] }];

            await _service.GetDefinitionsAndExamples(["walk"], AiMode.InputOnly);

            Assert.Equal(["walk"], _aiService.RequestedWords);
        }

        private static Word MakeWord(string value, params (string Meaning, int ExampleCount)[] definitions)
        {
            return new Word
            {
                Value = value,
                Definitions = definitions
                    .Select(def => new Definition
                    {
                        Value = def.Meaning,
                        Examples = Enumerable.Range(1, def.ExampleCount).Select(i => new Example { Value = $"example {i}" }).ToList()
                    })
                    .ToList()
            };
        }

        private class FakeWordSource : Domain.Interfaces.IParserService
        {
            public List<Word> Words { get; set; } = [];

            public Task<List<Word>> ParseWordDataAsync(string entry)
            {
                return Task.FromResult(Words);
            }
        }

        private class FakeAiService : IAiService
        {
            public Dictionary<WordMeaning, List<string>> Examples { get; set; } = new();
            public IReadOnlyCollection<WordMeaning>? RequestedMeanings { get; private set; }
            public int RequestedExamplesPerMeaning { get; private set; }
            public string[] RequestedWords { get; private set; } = [];
            public int RequestedExamplesPerNewWordMeaning { get; private set; }

            public Task<Dictionary<WordMeaning, List<string>>> GetExamplesAsync(IReadOnlyCollection<WordMeaning> meanings, int examplesPerMeaning)
            {
                RequestedMeanings = meanings;
                RequestedExamplesPerMeaning = examplesPerMeaning;

                return Task.FromResult(Examples);
            }

            public Task<List<Word>> GetMeaningsAndExamplesAsync(string[] words, int examplesPerMeaning)
            {
                RequestedWords = words;
                RequestedExamplesPerNewWordMeaning = examplesPerMeaning;

                return Task.FromResult(new List<Word>());
            }
        }
    }
}
