using Shared;
using System.Collections.ObjectModel;

namespace SmartBox.Mappers
{
    public class WordMapper : IMapper<Application.Models.Dictionary.Word, Models.Dictionary.Word>,
                              IMapper<Application.Models.Parser.Word, Models.Parser.Word>
    {
        private const string Separator = ", ";

        private readonly IMapper<Application.Models.Dictionary.PartOfSpeech, Models.Dictionary.PartOfSpeech> _partOfSpeechToUIDictionaryMapper;
        private readonly IMapper<Application.Models.Parser.PartOfSpeech, Models.Parser.PartOfSpeech> _partOfSpeechToUIParserMapper;

        public WordMapper(IMapper<Application.Models.Dictionary.PartOfSpeech, Models.Dictionary.PartOfSpeech> partOfSpeechToUIDictionaryMapper,
                          IMapper<Application.Models.Parser.PartOfSpeech, Models.Parser.PartOfSpeech> partOfSpeechToUIParserMapper)
        {
            _partOfSpeechToUIDictionaryMapper = partOfSpeechToUIDictionaryMapper;
            _partOfSpeechToUIParserMapper = partOfSpeechToUIParserMapper;
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
                Children = new ObservableCollection<Models.Parser.Meaning>()
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
    }
}
