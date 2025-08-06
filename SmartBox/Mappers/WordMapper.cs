using Shared;
using System.Collections.ObjectModel;

namespace SmartBox.Mappers
{
    public class WordMapper : IMapper<Application.Models.Dictionary.Word, Models.Dictionary.Word>,
                              IMapper<Application.Models.Parser.Word, Models.Parser.Word>,
                              IMapper<Models.Parser.WordPack, Application.Models.Parser.Word>
    {
        private const string Separator = ", ";

        private readonly IMapper<Application.Models.Dictionary.PartOfSpeech, Models.Dictionary.PartOfSpeech> _partOfSpeechToUIDictionaryMapper;
        private readonly IMapper<Application.Models.Parser.PartOfSpeech, Models.Parser.PartOfSpeech> _partOfSpeechToUIParserMapper;
        private readonly IMapper<Models.Parser.PartOfSpeech, Application.Models.Parser.PartOfSpeech> _partOfSpeechToAppParserMapper;

        public WordMapper(IMapper<Application.Models.Dictionary.PartOfSpeech, Models.Dictionary.PartOfSpeech> partOfSpeechToUIDictionaryMapper,
                          IMapper<Application.Models.Parser.PartOfSpeech, Models.Parser.PartOfSpeech> partOfSpeechToUIParserMapper,
                          IMapper<Models.Parser.PartOfSpeech, Application.Models.Parser.PartOfSpeech> partOfSpeechToAppParserMapper)
        {
            _partOfSpeechToUIDictionaryMapper = partOfSpeechToUIDictionaryMapper;
            _partOfSpeechToUIParserMapper = partOfSpeechToUIParserMapper;
            _partOfSpeechToAppParserMapper = partOfSpeechToAppParserMapper;
        }

        public Models.Dictionary.Word Map(Application.Models.Dictionary.Word source)
        {
            Models.Dictionary.PartOfSpeech partOfSpeech = _partOfSpeechToUIDictionaryMapper.Map(source.PartOfSpeech);

            return new Models.Dictionary.Word
            {
                Id = source.Id,
                Value = string.Join(Separator, source.Values),
                Popularity = source.Popularity,
                PartOfSpeech = partOfSpeech,
                ShouldStudy = source.ShouldStudy,
                Learned = source.Learned,
                Comments = source.Comments
            };
        }

        public Models.Parser.Word Map(Application.Models.Parser.Word source)
        {
            Models.Parser.Word word = new()
            {
                Area = source.Area,
                Value = source.Value,
                Type = _partOfSpeechToUIParserMapper.Map(source.Type),
                Children = new ObservableCollection<Models.Parser.Meaning>(),
                OtherForms = source.OtherForms.ToArray(),
                PluralForms = source.PluralForms.ToArray()
            };

            foreach (KeyValuePair<string, List<string>> meaningAndExamples in source.MeaningsAndExamples)
            {
                Models.Parser.Meaning meaning = new Models.Parser.Meaning
                {
                    Parent = word,
                    Value = meaningAndExamples.Key,
                    Children = new ObservableCollection<Models.Parser.Example>()
                };

                foreach (string example in meaningAndExamples.Value)
                {
                    meaning.Children.Add(new Models.Parser.Example
                    {
                        Value = example,
                        Parent = meaning
                    });
                }

                word.Children.Add(meaning);
            }

            return word;
        }

        public Application.Models.Parser.Word Map(Models.Parser.WordPack source)
        {
            Dictionary<string, List<string>> meaningsAndExamples = new();

            foreach (Models.Parser.Word word in source.Words)
            {
                foreach (Models.Parser.Meaning meaning in word.Children)
                {
                    List<string> examples = meaning.Children
                        .Select(example => example.Value)
                        .ToList();

                    if (meaningsAndExamples.ContainsKey(meaning.Value))
                    {
                        meaningsAndExamples[meaning.Value].AddRange(examples);
                    }
                    else
                    {
                        meaningsAndExamples[meaning.Value] = examples;
                    }
                }
            }

            return new Application.Models.Parser.Word()
            {
                Value = string.Join(", ", source.Words.Select(el => el.Value.ToLower()).Distinct()),
                Area = null,
                Type = source.Words.Any(el => el.Type == Models.Parser.PartOfSpeech.Verb || el.Type == Models.Parser.PartOfSpeech.PhrasalVerb) ?
                    Application.Models.Parser.PartOfSpeech.Verb :
                    Application.Models.Parser.PartOfSpeech.Other,
                OtherForms = source.Words
                    .SelectMany(el => el.OtherForms)
                    .ToHashSet(),
                PluralForms = source.Words.SelectMany(el => el.PluralForms)
                    .ToHashSet(),
                MeaningsAndExamples = meaningsAndExamples,
            };
        }
    }
}
