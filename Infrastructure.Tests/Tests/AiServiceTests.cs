using Domain.Models.Parser;
using Infrastructure.Services;

namespace Infrastructure.Tests.Tests
{
    public class AiServiceTests
    {
        private readonly AiService _aiService;

        public AiServiceTests()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _aiService = new AiService(null, null, 3, 3, 4);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        public static IEnumerable<object[]> GetMeaningsWithEmptyExamplesTestData()
        {
            yield return new object[]
            {
                new List<Word>()
                {
                    new Word()
                    {
                        Value = "eve",
                        Definitions = new()
                        {
                            new Definition()
                            {
                                Value = "The day or period before an important event",
                                Examples = new()
                                {
                                    new Example() { Value = "Mrs Clayton was speaking to Arab journalists in Paris on the eve of her visit to Egypt" },
                                    new Example() { Value = "Where did you spend Christmas Eve?" },
                                    new Example() { Value = "Sheena always throws a party on New Year's Eve" }
                                }
                            },
                            new Definition()
                            {
                                Value = "The evening",
                                Examples = new()
                            }
                        }
                    },
                    new Word()
                    {
                        Value = "Eve",
                        Definitions = new()
                        {
                            new Definition()
                            {
                                Value = "The first woman, according to the biblical story of how the world was made",
                                Examples = new()
                            }
                        }
                    },
                    new Word()
                    {
                        Value = "eve",
                        Definitions = new()
                        {
                            new Definition()
                            {
                                Value = "The evening or day before a holiday, or the period immediately before an important event",
                                Examples = new()
                                {
                                    new Example() { Value = "New Year's Eve" },
                                    new Example() { Value = "The eve of the election" }
                                }
                            }
                        }
                    }
                },
                new HashSet<(string word, string meaning)>
                {
                    ("eve", "The evening"),
                    ("Eve", "The first woman, according to the biblical story of how the world was made"),
                    ("eve", "The evening or day before a holiday, or the period immediately before an important event")
                }
            };

            yield return new object[]
            {
                new List<Word>()
                {
                    new Word()
                    {
                        Value = "eve",
                        Definitions = new()
                        {
                            new Definition()
                            {
                                Value = "The day or period before an important event",
                                Examples = new()
                                {
                                    new Example() { Value = "Mrs Clayton was speaking to Arab journalists in Paris on the eve of her visit to Egypt" },
                                    new Example() { Value = "Where did you spend Christmas Eve?" },
                                    new Example() { Value = "Sheena always throws a party on New Year's Eve" }
                                }
                            },
                            new Definition()
                            {
                                Value = "The evening",
                                Examples = new()
                                {
                                    new Example() { Value = "text example" }
                                }
                            }
                        }
                    },
                    new Word()
                    {
                        Value = "Eve",
                        Definitions = new()
                        {
                            new Definition()
                            {
                                Value = "The first woman, according to the biblical story of how the world was made",
                                Examples = new()
                                {
                                    new Example() { Value = "test example" }
                                }
                            }
                        }
                    },
                    new Word()
                    {
                        Value = "eve",
                        Definitions = new()
                        {
                            new Definition()
                            {
                                Value = "The evening or day before a holiday, or the period immediately before an important event",
                                Examples = new()
                                {
                                    new Example() { Value = "New Year's Eve" },
                                    new Example() { Value = "The eve of the election" }
                                }
                            }
                        }
                    }
                },
                new HashSet<(string word, string meaning)>
                {
                    ("eve", "The evening"),
                    ("Eve", "The first woman, according to the biblical story of how the world was made"),
                    ("eve", "The evening or day before a holiday, or the period immediately before an important event")
                }
            };

            yield return new object[]
            {
                new List<Word>()
                {
                    new Word()
                    {
                        Value = "test",
                        Definitions = new()
                        {
                            new Definition()
                            {
                                Value = "A procedure intended to establish the quality, performance, or reliability of something",
                                Examples = new()
                                {
                                    new Example() { Value = "Example 1" },
                                    new Example() { Value = "Example 2" },
                                    new Example() { Value = "Example 3" }
                                }
                            }
                        }
                    }
                },
                new HashSet<(string word, string meaning)>()
            };
        }

        [Theory]
        [MemberData(nameof(GetMeaningsWithEmptyExamplesTestData))]
        public void GetMeaningsWithEmptyExamples_WordsWithMeaningsWithoutEnoughExamples_ReturnsExpectedEmptyMeanings(
            IEnumerable<Word> input,
            HashSet<(string word, string meaning)> expectedResult)
        {
            // Act
            HashSet<(string word, string meaning)> result = _aiService.GetMeaningsWithEmptyExamples(input);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void FillMissingExamples_MatchingDefinition_AddsExamples()
        {
            var words = new List<Word>
            {
                new Word
                {
                    Value = "reek",
                    Definitions = new()
                    {
                        new Definition
                        {
                            Value = "to have a strong unpleasant smell",
                            Examples = new() { new Example { Value = "existing example" } }
                        }
                    }
                }
            };

            var meaningsAndExamples = new Dictionary<string, List<string>>
            {
                ["to have a strong unpleasant smell"] = new() { "Her breath reeked of garlic", "The garbage reeked" }
            };

            _aiService.FillMissingExamples(meaningsAndExamples, words);

            Assert.Equal(3, words[0].Definitions[0].Examples.Count);
            Assert.Equal("existing example", words[0].Definitions[0].Examples[0].Value);
            Assert.Equal("Her breath reeked of garlic", words[0].Definitions[0].Examples[1].Value);
            Assert.Equal("The garbage reeked", words[0].Definitions[0].Examples[2].Value);
        }

        [Fact]
        public void FillMissingExamples_NoMatchingDefinition_ExamplesUnchanged()
        {
            var words = new List<Word>
            {
                new Word
                {
                    Value = "reek",
                    Definitions = new()
                    {
                        new Definition
                        {
                            Value = "to have a strong unpleasant smell",
                            Examples = new() { new Example { Value = "only one" } }
                        }
                    }
                }
            };

            var meaningsAndExamples = new Dictionary<string, List<string>>
            {
                ["completely different definition"] = new() { "example 1", "example 2" }
            };

            _aiService.FillMissingExamples(meaningsAndExamples, words);

            Assert.Single(words[0].Definitions[0].Examples);
        }

        [Fact]
        public void FillMissingExamples_WordAlreadyHasEnoughExamples_Skipped()
        {
            var words = new List<Word>
            {
                new Word
                {
                    Value = "reek",
                    Definitions = new()
                    {
                        new Definition
                        {
                            Value = "to have a strong unpleasant smell",
                            Examples = new()
                            {
                                new Example { Value = "example 1" },
                                new Example { Value = "example 2" },
                                new Example { Value = "example 3" }
                            }
                        }
                    }
                }
            };

            var meaningsAndExamples = new Dictionary<string, List<string>>
            {
                ["to have a strong unpleasant smell"] = new() { "should not be added" }
            };

            _aiService.FillMissingExamples(meaningsAndExamples, words);

            Assert.Equal(3, words[0].Definitions[0].Examples.Count);
        }

        [Fact]
        public void FillMissingExamples_MultipleWordsAndDefinitions_FillsCorrectly()
        {
            var words = new List<Word>
            {
                new Word
                {
                    Value = "eve",
                    Definitions = new()
                    {
                        new Definition
                        {
                            Value = "The day before an important event",
                            Examples = new()
                            {
                                new Example { Value = "Christmas Eve" },
                                new Example { Value = "New Year's Eve" },
                                new Example { Value = "On the eve of the election" }
                            }
                        },
                        new Definition
                        {
                            Value = "The evening",
                            Examples = new()
                        }
                    }
                },
                new Word
                {
                    Value = "Eve",
                    Definitions = new()
                    {
                        new Definition
                        {
                            Value = "The first woman",
                            Examples = new()
                        }
                    }
                }
            };

            var meaningsAndExamples = new Dictionary<string, List<string>>
            {
                ["The evening"] = new() { "A warm summer eve", "On that fateful eve" },
                ["The first woman"] = new() { "Adam and Eve", "Eve was created from a rib" }
            };

            _aiService.FillMissingExamples(meaningsAndExamples, words);

            // First word, first definition — already had 3, untouched
            Assert.Equal(3, words[0].Definitions[0].Examples.Count);
            // First word, second definition — was empty, got 2
            Assert.Equal(2, words[0].Definitions[1].Examples.Count);
            // Second word — was empty, got 2
            Assert.Equal(2, words[1].Definitions[0].Examples.Count);
        }
    }
}