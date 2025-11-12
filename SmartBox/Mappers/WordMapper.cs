using Shared;
using System.Collections.ObjectModel;

namespace SmartBox.Mappers
{
    public class WordMapper : IMapper<Domain.Models.Dictionary.Word, SmartBox.Models.Dictionary.Word>,
                              IMapper<Domain.Models.Parser.Word, SmartBox.Models.Parser.Word>,
                              IMapper<SmartBox.Models.Parser.WordPack, Domain.Models.Parser.Word>
    {
        private const string Separator = ", ";

        private readonly IMapper<Domain.Models.Dictionary.PartOfSpeech, Models.Dictionary.PartOfSpeech> _partOfSpeechToUIDictionaryMapper;
        private readonly IMapper<Domain.Models.Parser.PartOfSpeech, Models.Parser.PartOfSpeech> _partOfSpeechToUIParserMapper;
        private readonly IMapper<Models.Parser.PartOfSpeech, Domain.Models.Parser.PartOfSpeech> _partOfSpeechToAppParserMapper;

        public WordMapper(IMapper<Domain.Models.Dictionary.PartOfSpeech, Models.Dictionary.PartOfSpeech> partOfSpeechToUIDictionaryMapper,
                          IMapper<Domain.Models.Parser.PartOfSpeech, Models.Parser.PartOfSpeech> partOfSpeechToUIParserMapper,
                          IMapper<Models.Parser.PartOfSpeech, Domain.Models.Parser.PartOfSpeech> partOfSpeechToAppParserMapper)
        {
            _partOfSpeechToUIDictionaryMapper = partOfSpeechToUIDictionaryMapper;
            _partOfSpeechToUIParserMapper = partOfSpeechToUIParserMapper;
            _partOfSpeechToAppParserMapper = partOfSpeechToAppParserMapper;
        }

        public Models.Dictionary.Word Map(Domain.Models.Dictionary.Word source)
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

        public Models.Parser.Word Map(Domain.Models.Parser.Word source)
        {
            Models.Parser.Word word = new()
            {
                Id = source.Id,
                Area = source.Area,
                Value = source.Value,
                Type = _partOfSpeechToUIParserMapper.Map(source.Type),
                Children = new ObservableCollection<Models.Parser.Meaning>(),
                OtherForms = source.OtherForms.ToArray(),
                PluralForms = source.PluralForms.ToArray()
            };

            foreach (Domain.Models.Parser.Definition definition in source.Definitions)
            {
                Models.Parser.Meaning meaning = new Models.Parser.Meaning
                {
                    Parent = word,
                    Value = definition.Value.ToUpperFirstChar(),
                    Id = definition.Id,
                    Children = new ObservableCollection<Models.Parser.Example>()
                };

                foreach (Domain.Models.Parser.Example example in definition.Examples)
                {
                    meaning.Children.Add(new Models.Parser.Example
                    {
                        Value = example.Value.ToUpperFirstChar(),
                        Id = example.Id,
                        Parent = meaning,
                    });
                }

                word.Children.Add(meaning);
            }

            return word;
        }

        public Domain.Models.Parser.Word Map(SmartBox.Models.Parser.WordPack source)
        {
            List<Domain.Models.Parser.Definition> definitions = new List<Domain.Models.Parser.Definition>();

            foreach (Models.Parser.Word word in source.Words)
            {
                foreach (Models.Parser.Meaning meaning in word.Children)
                {
                    List<Domain.Models.Parser.Example> examples = meaning.Children
                        .Select(example => new Domain.Models.Parser.Example
                        {
                            Id = example.Id,
                            Value = example.Value.ToLowerFirstChar(),
                        })
                        .ToList();

                    definitions.Add(new Domain.Models.Parser.Definition
                    {
                        Id = meaning.Id,
                        Value = meaning.Value.ToLowerFirstChar(),
                        Examples = examples
                    });
                }
            }

            return new Domain.Models.Parser.Word()
            {
                Value = string.Join(", ", source.Words.Select(el => el.Value.ToLower()).Distinct()),
                Area = null,
                Type = source.Words.Any(el => el.Type == Models.Parser.PartOfSpeech.Verb || el.Type == Models.Parser.PartOfSpeech.PhrasalVerb) ?
                    Domain.Models.Parser.PartOfSpeech.Verb :
                    Domain.Models.Parser.PartOfSpeech.Other,
                OtherForms = source.Words
                    .SelectMany(el => el.OtherForms)
                    .ToHashSet(),
                PluralForms = source.Words.SelectMany(el => el.PluralForms)
                    .ToHashSet(),
                Definitions = definitions
            };
        }
    }
}
