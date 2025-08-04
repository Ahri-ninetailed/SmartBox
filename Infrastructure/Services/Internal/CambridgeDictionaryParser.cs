using AngleSharp;
using AngleSharp.Dom;
using Infrastructure.Models.Parser;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Infrastructure.Services.Internal
{
    // TODO: когда нибудь переписать это дело
    public class CambridgeDictionaryParser : IParser
    {
        private readonly ILogger<CambridgeDictionaryParser> _logger;

        private readonly IBrowsingContext _context;

        public CambridgeDictionaryParser(ILogger<CambridgeDictionaryParser> logger)
        {
            _logger = logger;

            _context = BrowsingContext.New(Configuration.Default);
        }

        private const string MEANING_TAGS = ".def.ddef_d.db";
        private const string MEANINGS_AND_EXAMPLES_TAGS = ".def-block.ddef_block";
        private const string EXAMPLES_TAGS = ".eg.deg";
        private const string BLUE_RECTANGLE_BLOCK_TAGS = ".pr.phrase-block.dphrase-block";
        private const string BLUE_RECTANGLE_HEADER_TAGS = ".phrase-title.dphrase-title";
        private readonly string[] _partsOfSpeech = { "verb", "phrasal verb", "idiom", "phrase" };

        public async Task<List<Word>> ExtractWordDataAsync(string html)
        {
            IDocument page = await _context.OpenAsync(req => req.Content(html));
            IHtmlCollection<IElement> areasOfWords = page.QuerySelectorAll(".pr.di.superentry");

            Dictionary<(string word, PartOfSpeech type, string? area), Word> wordsData = new();

            foreach (IElement areaOfWord in areasOfWords)
            {
                IHtmlCollection<IElement> cards = areaOfWord.QuerySelectorAll(".pr.entry-body__el");
                IHtmlCollection<IElement> phrasalVerbCards = areaOfWord.QuerySelectorAll(".pv-block");
                IHtmlCollection<IElement> idiomCards = areaOfWord.QuerySelectorAll(".pr.idiom-block");

                string? area = GetWordArea(areaOfWord);

                using (LogContext.PushProperty("Область", area))
                {
                    foreach (IElement card in phrasalVerbCards)
                    {
                        Word word = GetPhrasalVerb(card);
                        word.Area = area;

                        AddWordToDictionary(wordsData, word, area);
                    }

                    foreach (IElement card in idiomCards)
                    {
                        Word word = GetIdiom(card);
                        word.Area = area;

                        AddWordToDictionary(wordsData, word, area);
                    }

                    foreach (IElement card in cards)
                    {
                        Word word = GetWord(card);
                        word.Area = area;

                        AddWordToDictionary(wordsData, word, area);
                    }

                    Word[] blueRectangles = GetBlueRecrangles(areaOfWord, area);
                    if (blueRectangles.Length != 0)
                    {
                        foreach (Word blueRectangle in blueRectangles)
                        {
                            AddWordToDictionary(wordsData, blueRectangle, area);
                        }
                    }
                }
            }

            return wordsData.Values.ToList();
        }

        private Word[] GetBlueRecrangles(IElement areaOfWord, string? area)
        {
            IHtmlCollection<IElement> blueRectanglesTags = areaOfWord.QuerySelectorAll(BLUE_RECTANGLE_BLOCK_TAGS);

            if (blueRectanglesTags.Length == 0)
            {
                return Array.Empty<Word>();
            }

            Word[] result = new Word[blueRectanglesTags.Length];

            for (int i = 0; i < blueRectanglesTags.Length; i++)
            {
                IElement? valueTag = blueRectanglesTags[i].QuerySelector(BLUE_RECTANGLE_HEADER_TAGS);
                if (valueTag == null)
                {
                    throw new Exception("Нет тега с значением фразы");
                }

                string value = valueTag.TextContent.Trim();
                result[i] = new Word()
                {
                    Value = value,
                    Type = PartOfSpeech.BlueRectangle,
                    Area = area,
                };

                IHtmlCollection<IElement> meaningsAndExamplesTags = blueRectanglesTags[i].QuerySelectorAll(MEANINGS_AND_EXAMPLES_TAGS);

                using (LogContext.PushProperty("Слово", result[i].Value))
                using (LogContext.PushProperty("Тип", result[i].Type))
                {
                    foreach (IElement meaningAndExamplesTag in meaningsAndExamplesTags)
                    {
                        FillMeaningsAndExamples(meaningAndExamplesTag, result[i].MeaningsAndExamples);
                    }
                }
            }

            return result;
        }

        private void FillMeaningsAndExamples(IElement meaningAndExamplesTag, Dictionary<string, List<string>> dictionary)
        {
            IElement? meaningTag = meaningAndExamplesTag.QuerySelector(MEANING_TAGS);
            if (meaningTag == null)
            {
                _logger.LogInformation("Нет тега с определением");
                return;
            }

            string meaning = meaningTag.TextContent.Trim();
            meaning = Regex.Replace(meaning, @"\s+", " ");
            meaning = CapitalizeFirstLetter(meaning);
            meaning = RemoveEndingDotsAndColonsAndSpaces(meaning);

            IHtmlCollection<IElement> examplesTags = meaningAndExamplesTag.QuerySelectorAll(EXAMPLES_TAGS);
            string[] examples = examplesTags.Select(el => el.TextContent.Trim()).Where(el => string.IsNullOrEmpty(el) == false).ToArray();
            for (int i = 0; i < examples.Length; i++)
            {
                examples[i] = CapitalizeFirstLetter(examples[i]);
                examples[i] = RemoveEndingDotsAndColonsAndSpaces(examples[i]);
            }
            if (dictionary.ContainsKey(meaning))
            {
                dictionary[meaning].AddRange(examples);
            }
            else
            {
                dictionary.Add(meaning, examples.ToList());
            }
        }

        private string? GetWordArea(IElement areaOfWord)
        {
            IElement? areaTag = areaOfWord.QuerySelector(".c_hh");
            string? area = null;
            if (areaTag != null)
            {
                int indexOfFlag = areaTag.TextContent.IndexOf("|");
                area = areaTag.TextContent.Substring(indexOfFlag + 1).Trim();
            }

            return area;
        }

        private Word GetWord(IElement card)
        {
            IElement? cardWordAndAdditionalData = card.QuerySelector(".pos-header.dpos-h");
            if (cardWordAndAdditionalData == null)
            {
                throw new Exception("Нет заголовка слова с доп. данными");
            }

            IElement? word = cardWordAndAdditionalData.QuerySelector(".hw.dhw");
            if (word == null || string.IsNullOrWhiteSpace(word.TextContent))
            {
                throw new Exception("Нет заголовка со словом");
            }

            PartOfSpeech type = GetPartOfSpeech(cardWordAndAdditionalData);

            IEnumerable<string> pluralForms = GetPluralForms(cardWordAndAdditionalData);

            IEnumerable<string> otherForms = GetOtherForms(cardWordAndAdditionalData);

            Dictionary<string, List<string>> meaningsAndExamples;

            using (LogContext.PushProperty("Слово", word.TextContent))
            using (LogContext.PushProperty("Тип", type))
            {
                meaningsAndExamples = GetMeaningsAndExamples(card);
            }

            return new Word()
            {
                Value = word.TextContent.Trim(),
                Type = type,
                PluralForms = pluralForms.ToHashSet(),
                OtherForms = otherForms.ToHashSet(),
                MeaningsAndExamples = meaningsAndExamples
            };
        }

        private Word GetPhrasalVerb(IElement card)
        {
            IElement? rawPhrasalVerbTitle = card.QuerySelector(".di-title");
            IElement? phrasalVerbTitle = rawPhrasalVerbTitle?.QuerySelector("b");
            if (phrasalVerbTitle == null)
            {
                throw new Exception("Нет заголовка фразового глагола");
            }

            PartOfSpeech type = PartOfSpeech.PhrasalVerb;

            Dictionary<string, List<string>> meaningsAndExamples;

            using (LogContext.PushProperty("Слово", phrasalVerbTitle.TextContent))
            using (LogContext.PushProperty("Тип", type))
            {
                meaningsAndExamples = GetMeaningsAndExamples(card);
            }

            return new Word()
            {
                Value = phrasalVerbTitle.TextContent,
                Type = type,
                PluralForms = new HashSet<string>(),
                OtherForms = new HashSet<string>(),
                MeaningsAndExamples = meaningsAndExamples,
            };
        }

        private Word GetIdiom(IElement card)
        {
            IElement? rawIdiomTitle = card.QuerySelector(".headword.tw-bw.dhw.dpos-h_hw");
            IElement? idiomTitle = rawIdiomTitle?.QuerySelector("b");
            if (idiomTitle == null)
            {
                throw new Exception("Нет заголовка фразового глагола");
            }

            IEnumerable<string> otherForms = GetOtherForms(card);

            PartOfSpeech type = PartOfSpeech.Idiom;

            Dictionary<string, List<string>> meaningsAndExamples;

            using (LogContext.PushProperty("Слово", idiomTitle.TextContent))
            using (LogContext.PushProperty("Тип", type))
            {
                meaningsAndExamples = GetMeaningsAndExamples(card);
            }

            return new Word()
            {
                Value = idiomTitle.TextContent,
                Type = type,
                PluralForms = new HashSet<string>(),
                OtherForms = otherForms.ToHashSet(),
                MeaningsAndExamples = meaningsAndExamples,
            };
        }

        private Dictionary<string, List<string>> GetMeaningsAndExamples(IElement card)
        {
            IHtmlCollection<IElement> rawMeaningAndExamplesTags = card.QuerySelectorAll(MEANINGS_AND_EXAMPLES_TAGS);
            if (rawMeaningAndExamplesTags.Length == 0)
            {
                throw new Exception("У слова нет определений и примеров использования");
            }

            string rawBlueRectangleBlockClasses = BLUE_RECTANGLE_BLOCK_TAGS.Replace(".", " ").Trim();
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();
            foreach (IElement rawMeaningsAndExamplesTag in rawMeaningAndExamplesTags)
            {
                if (rawMeaningsAndExamplesTag.Ancestors<IElement>().Any(el => el.ClassName?.Contains(rawBlueRectangleBlockClasses) ?? false))
                {
                    continue;
                }

                FillMeaningsAndExamples(rawMeaningsAndExamplesTag, result);
            }

            return result;
        }

        private IEnumerable<string> GetOtherForms(IElement cardWordAndAdditionalData)
        {
            IHtmlCollection<IElement> otherFormsTags = cardWordAndAdditionalData.QuerySelectorAll(".v.dv.lmr-0");
            if (otherFormsTags.Length == 0)
            {
                return Enumerable.Empty<string>();
            }

            IEnumerable<string> result = otherFormsTags.Select(el => el.TextContent);
            return result;
        }

        private PartOfSpeech GetPartOfSpeech(IElement cardWordAndAdditionalData)
        {
            IHtmlCollection<IElement> tagsWithTypes = cardWordAndAdditionalData.QuerySelectorAll(".pos.dpos");
            if (tagsWithTypes == null || tagsWithTypes.Length == 0)
            {
                _logger.LogInformation("Нет тегов с частью речи");
                return PartOfSpeech.Unknown;
            }

            IEnumerable<string?> types = tagsWithTypes.Select(el => el?.TextContent.ToLower());

            if (types.Any(el => _partsOfSpeech.Contains(el)))
            {
                return PartOfSpeech.Verb;
            }

            return PartOfSpeech.Noun;
        }

        private IEnumerable<string> GetPluralForms(IElement cardWordAndAdditionalData)
        {
            IElement? pluralMarker = cardWordAndAdditionalData.QuerySelector(".lab.dlab");
            if (pluralMarker?.TextContent != "plural")
            {
                return Enumerable.Empty<string>();
            }

            IHtmlCollection<IElement> pluralTags = cardWordAndAdditionalData.QuerySelectorAll(".inf.dinf");
            IEnumerable<string> result = pluralTags.Select(el => el.TextContent);
            return result;
        }

        private void AddWordToDictionary(Dictionary<(string word, PartOfSpeech type, string? area), Word> dictionary, Word word, string? area)
        {
            (string word, PartOfSpeech type, string? area) wordKey = (word.Value, word.Type, area);
            if (dictionary.ContainsKey(wordKey))
            {
                Summarize(dictionary[wordKey], word);
            }
            else
            {
                dictionary.Add(wordKey, word);
            }
        }

        private void Summarize(Word target ,Word source)
        {
            foreach (KeyValuePair<string, List<string>> pare in source.MeaningsAndExamples)
            {
                if (target.MeaningsAndExamples.ContainsKey(pare.Key))
                {
                    target.MeaningsAndExamples[pare.Key].AddRange(pare.Value);
                }
                else
                {
                    target.MeaningsAndExamples.Add(pare.Key, pare.Value);
                }
            }
            target.PluralForms.UnionWith(source.PluralForms);
            target.OtherForms.UnionWith(source.OtherForms);
        }

        private string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return char.ToUpper(input[0]) + input.Substring(1);
        }

        private string RemoveEndingDotsAndColonsAndSpaces(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            input = Regex.Replace(input, @"[\s.:]+$", "");

            return input;
        }
    }
}
