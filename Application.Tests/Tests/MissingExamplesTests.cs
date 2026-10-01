using Application.Services;
using Domain.Models.Parser;

namespace Application.Tests.Tests
{
    public class MissingExamplesTests
    {
        private const int FillIfBelow = 3;

        public static IEnumerable<object[]> FindTestData()
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
                new HashSet<WordMeaning>
                {
                    new WordMeaning("eve", "The evening"),
                    new WordMeaning("Eve", "The first woman, according to the biblical story of how the world was made"),
                    new WordMeaning("eve", "The evening or day before a holiday, or the period immediately before an important event")
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
                new HashSet<WordMeaning>
                {
                    new WordMeaning("eve", "The evening"),
                    new WordMeaning("Eve", "The first woman, according to the biblical story of how the world was made"),
                    new WordMeaning("eve", "The evening or day before a holiday, or the period immediately before an important event")
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
                new HashSet<WordMeaning>()
            };
        }

        [Theory]
        [MemberData(nameof(FindTestData))]
        public void Find_WordsWithMeaningsWithoutEnoughExamples_ReturnsThoseMeanings(
            IEnumerable<Word> input,
            HashSet<WordMeaning> expectedResult)
        {
            // Act
            HashSet<WordMeaning> result = MissingExamples.Find(input, FillIfBelow);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void Add_MatchingDefinition_AddsExamples()
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

            var meaningsAndExamples = new Dictionary<WordMeaning, List<string>>
            {
                [new WordMeaning("reek", "to have a strong unpleasant smell")] = new() { "Her breath reeked of garlic", "The garbage reeked" }
            };

            MissingExamples.Add(words, meaningsAndExamples, FillIfBelow);

            Assert.Equal(3, words[0].Definitions[0].Examples.Count);
            Assert.Equal("existing example", words[0].Definitions[0].Examples[0].Value);
            Assert.Equal("Her breath reeked of garlic", words[0].Definitions[0].Examples[1].Value);
            Assert.Equal("The garbage reeked", words[0].Definitions[0].Examples[2].Value);
        }

        [Fact]
        public void Add_NoMatchingDefinition_ExamplesUnchanged()
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

            var meaningsAndExamples = new Dictionary<WordMeaning, List<string>>
            {
                [new WordMeaning("reek", "completely different definition")] = new() { "example 1", "example 2" }
            };

            MissingExamples.Add(words, meaningsAndExamples, FillIfBelow);

            Assert.Single(words[0].Definitions[0].Examples);
        }

        [Fact]
        public void Add_WordAlreadyHasEnoughExamples_Skipped()
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

            var meaningsAndExamples = new Dictionary<WordMeaning, List<string>>
            {
                [new WordMeaning("reek", "to have a strong unpleasant smell")] = new() { "should not be added" }
            };

            MissingExamples.Add(words, meaningsAndExamples, FillIfBelow);

            Assert.Equal(3, words[0].Definitions[0].Examples.Count);
        }

        [Fact]
        public void Add_MultipleWordsAndDefinitions_FillsCorrectly()
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

            var meaningsAndExamples = new Dictionary<WordMeaning, List<string>>
            {
                [new WordMeaning("eve", "The evening")] = new() { "A warm summer eve", "On that fateful eve" },
                [new WordMeaning("Eve", "The first woman")] = new() { "Adam and Eve", "Eve was created from a rib" }
            };

            MissingExamples.Add(words, meaningsAndExamples, FillIfBelow);

            // First word, first definition — already had 3, untouched
            Assert.Equal(3, words[0].Definitions[0].Examples.Count);
            // First word, second definition — was empty, got 2
            Assert.Equal(2, words[0].Definitions[1].Examples.Count);
            // Second word — was empty, got 2
            Assert.Equal(2, words[1].Definitions[0].Examples.Count);
        }
    }
}