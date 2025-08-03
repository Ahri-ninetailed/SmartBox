using Application.Models;

using Shared;

namespace Application.Mapper
{
    public class WordMapper : IMapper<Domain.Models.Dictionary.Word, Word>,
                              IMapper<Word, Domain.Models.Dictionary.Word>
    {
        private readonly IMapper<Domain.Models.Dictionary.PartOfSpeech, PartOfSpeech> _partOfSpeechToAppMapper;
        private readonly IMapper<PartOfSpeech, Domain.Models.Dictionary.PartOfSpeech> _partOfSpeechToDomMapper;

        public WordMapper(IMapper<Domain.Models.Dictionary.PartOfSpeech, PartOfSpeech> partOfSpeechToAppMapper,
                          IMapper<PartOfSpeech, Domain.Models.Dictionary.PartOfSpeech> partOfSpeechToDomMapper)
        {
            _partOfSpeechToDomMapper = partOfSpeechToDomMapper;
            _partOfSpeechToAppMapper = partOfSpeechToAppMapper;
        }

        public Word Map(Domain.Models.Dictionary.Word source)
        {
            return new Word()
            {
                Id = source.Id,
                Values = source.Values,
                Popularity = source.Popularity,
                PartOfSpeech = _partOfSpeechToAppMapper.Map(source.PartOfSpeech),
                ShouldStudy = source.ShouldStudy,
                Learned = source.Learned,
                Comments = source.Comments,
            };
        }

        public Domain.Models.Dictionary.Word Map(Word source)
        {
            return new Domain.Models.Dictionary.Word()
            {
                Id = source.Id,
                Values = source.Values,
                Popularity = source.Popularity,
                PartOfSpeech = _partOfSpeechToDomMapper.Map(source.PartOfSpeech),
                ShouldStudy = source.ShouldStudy,
                Learned = source.Learned,
                Comments = source.Comments,
            };
        }
    }
}
