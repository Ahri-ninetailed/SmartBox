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
                        MeaningsAndExamples = new()
                        {
                            {
                                "The day or period before an important event",
                                new()
                                {
                                    "Mrs Clayton was speaking to Arab journalists in Paris on the eve of her visit to Egypt",
                                    "Where did you spend Christmas Eve?",
                                    "Sheena always throws a party on New Year's Eve"
                                }
                            },
                            {
                                "The evening",
                                new()
                            }
                        }
                    },
                    new Word()
                    {
                        Value = "Eve",
                        MeaningsAndExamples = new()
                        {
                            {
                                "The first woman, according to the biblical story of how the world was made",
                                new()
                            }
                        }
                    },
                    new Word()
                    {
                        Value = "eve",
                        MeaningsAndExamples = new()
                        {
                            {
                                "The evening or day before a holiday, or the period immediately before an important event",
                                new()
                                {
                                    "New Year's Eve",
                                    "The eve of the election"
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
                        MeaningsAndExamples = new()
                        {
                            {
                                "The day or period before an important event",
                                new()
                                {
                                    "Mrs Clayton was speaking to Arab journalists in Paris on the eve of her visit to Egypt",
                                    "Where did you spend Christmas Eve?",
                                    "Sheena always throws a party on New Year's Eve"
                                }
                            },
                            {
                                "The evening",
                                new()
                                {
                                    "text example"
                                }
                            }
                        }
                    },
                    new Word()
                    {
                        Value = "Eve",
                        MeaningsAndExamples = new()
                        {
                            {
                                "The first woman, according to the biblical story of how the world was made",
                                new()
                                {
                                    "test example"
                                }
                            }
                        }
                    },
                    new Word()
                    {
                        Value = "eve",
                        MeaningsAndExamples = new()
                        {
                            {
                                "The evening or day before a holiday, or the period immediately before an important event",
                                new()
                                {
                                    "New Year's Eve",
                                    "The eve of the election"
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
                        MeaningsAndExamples = new()
                        {
                            {
                                "A procedure intended to establish the quality, performance, or reliability of something",
                                new()
                                {
                                    "Example 1",
                                    "Example 2",
                                    "Example 3"
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