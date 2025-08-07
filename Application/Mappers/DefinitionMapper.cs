using Shared;

namespace Application.Mappers
{
    public class DefinitionMapper : IMapper<Application.Models.Parser.Definition, Domain.Models.Dictionary.Definition>
    {
        public Domain.Models.Dictionary.Definition Map(Models.Parser.Definition source)
        {
            return new Domain.Models.Dictionary.Definition()
            {
                Value = source.Value,
                Examples = source.Examples.Select(example =>
                {
                    return new Domain.Models.Dictionary.Example()
                    {
                        Value = example.Value
                    };
                }).ToList(),
            };
        }
    }
}
