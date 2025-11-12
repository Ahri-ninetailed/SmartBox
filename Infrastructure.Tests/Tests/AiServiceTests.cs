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
            _aiService = new AiService(null);
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
    }
}