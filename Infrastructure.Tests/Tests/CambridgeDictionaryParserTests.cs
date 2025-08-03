using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.Tests
{
    public class CambridgeDictionaryParserTests
    {
        private readonly Interfaces.IParser _parser;

        public CambridgeDictionaryParserTests()
        {
            var services = new ServiceCollection();
            services.AddInfrastructureServices();
            var provider = services.BuildServiceProvider();

            _parser = provider.GetRequiredService<Interfaces.IParser>();
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public async Task GetParsedWords_WithVariousHtmlInputs_ReturnsExpectedWordsAsync(string testFileName, MemberDataSerializer<IEnumerable<Models.Parser.Word>> expectedResult)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), $@"TestData\{testFileName}.html");
            string html = File.ReadAllText(filePath);

            IEnumerable<Models.Parser.Word> result = await _parser.GetParsedWords(html);

            result = result.OrderBy(el => el.Area).ThenBy(el => el.Type).ThenBy(el => el.Value).ToArray();
            foreach (var element in result)
            {
                element.MeaningsAndExamples = element.MeaningsAndExamples
                    .OrderBy(el => el.Key)
                    .ThenBy(el => string.Join(",", el.Value.OrderBy(el => el)))
                    .ToDictionary();
            }

            Models.Parser.Word[] orderedExpectedResult = expectedResult.Object.OrderBy(el => el.Area).ThenBy(el => el.Type).ThenBy(el => el.Value).ToArray();
            foreach (var element in orderedExpectedResult)
            {
                element.MeaningsAndExamples = element.MeaningsAndExamples
                    .OrderBy(el => el.Key)
                    .ThenBy(el => string.Join(",", el.Value.OrderBy(el => el)))
                    .ToDictionary();
            }

            Assert.Equal(orderedExpectedResult, result, new WordEqualityComparer());
        }

        public static IEnumerable<object[]> TestData()
        {
            yield return new object[]
            {
                "AllTheSame", new MemberDataSerializer<IEnumerable<Models.Parser.Word>>(new List<Models.Parser.Word>()
                {
                    new Models.Parser.Word()
                    {
                        Value = "all the same",
                        Type = Models.Parser.PartsOfSpeech.Idiom,
                        Area = null,
                        MeaningsAndExamples = new Dictionary<string, List<string>>()
                        {
                            {
                                "Despite what has just been said",
                                new List<string>()
                                {
                                    "It rained every day of our holiday - but we had a good time all the same",
                                }
                            }
                        }
                    }
                })
            };
        }
    }

    public class WordEqualityComparer : IEqualityComparer<Models.Parser.Word>
    {
        public bool Equals(Models.Parser.Word? x, Models.Parser.Word? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x is null || y is null)
                return false;

            bool value = x.Value == y.Value;
            bool type = x.Type == y.Type;
            bool area = x.Area == y.Area;
            bool meaningsAndExamples = AreDictionariesEqual(x.MeaningsAndExamples, y.MeaningsAndExamples);
            bool pluralForms = x.PluralForms.SetEquals(y.PluralForms);
            bool otherForms = x.OtherForms.SetEquals(y.OtherForms);

            return value &&
                   type &&
                   area &&
                   meaningsAndExamples &&
                   pluralForms &&
                   otherForms;
        }

        private bool AreDictionariesEqual(Dictionary<string, List<string>> dict1, Dictionary<string, List<string>> dict2)
        {
            // Сначала проверяем, что количество элементов в словарях одинаковое
            if (dict1.Count != dict2.Count)
            {
                return false;
            }

            // Проходим по всем ключам и значениям первого словаря
            foreach (var kvp in dict1)
            {
                // Проверяем, существует ли ключ во втором словаре
                if (!dict2.TryGetValue(kvp.Key, out var list2))
                {
                    return false;
                }

                // Проверяем, что списки значений равны по длине
                var list1 = kvp.Value;
                if (list1.Count != list2.Count)
                {
                    return false;
                }

                // Проверяем, что все элементы списков совпадают
                if (!kvp.Value.OrderBy(x => x).SequenceEqual(list2.OrderBy(x => x)))
                {
                    return false;
                }
            }

            // Если все проверки пройдены, возвращаем true
            return true;
        }

        public int GetHashCode(Models.Parser.Word obj)
        {
            int hashCode = obj.Value?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ obj.Type.GetHashCode();
            hashCode = (hashCode * 397) ^ (obj.Area?.GetHashCode() ?? 0);

            hashCode = (hashCode * 397) ^ obj.MeaningsAndExamples.Aggregate(0, (acc, kvp) =>
                acc ^ kvp.Key.GetHashCode() ^ kvp.Value.Aggregate(0, (acc2, ex) => acc2 ^ ex.GetHashCode()));

            hashCode = (hashCode * 397) ^ obj.PluralForms.Aggregate(0, (acc, p) => acc ^ p.GetHashCode());
            hashCode = (hashCode * 397) ^ obj.OtherForms.Aggregate(0, (acc, o) => acc ^ o.GetHashCode());
            return hashCode;
        }
    }
}
