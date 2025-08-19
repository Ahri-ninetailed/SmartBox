using Shared;

namespace Application.Mappers
{
    public class DefinitionMapper : IMapper<Application.Models.Parser.Definition, Domain.Models.Dictionary.Definition>
    {
        public Domain.Models.Dictionary.Definition Map(Models.Parser.Definition source)
        {
            return new Domain.Models.Dictionary.Definition()
            {
                Id = source.Id,
                Value = source.Value,
                Examples = source.Examples.Select(example =>
                {
                    return new Domain.Models.Dictionary.Example()
                    {
                        Id = example.Id,
                        DefinitionId = source.Id,
                        Value = example.Value
                    };
                }).ToList(),
            };
        }
    }
}
