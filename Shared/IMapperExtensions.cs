namespace Shared
{
    public static class IMapperExtensions
    {
        private const string NullSourcesError = "Source collection cannot be null.";

        public static IEnumerable<TDestination> Map<TSource, TDestination>(
            this IMapper<TSource, TDestination> mapper,
            IEnumerable<TSource> sources)
        {
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources), NullSourcesError);
            }

            return sources.Select(mapper.Map);
        }

        public static List<TDestination> MapToList<TSource, TDestination>(
            this IMapper<TSource, TDestination> mapper,
            IEnumerable<TSource> sources)
        {
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources), NullSourcesError);
            }

            return sources.Select(mapper.Map).ToList();
        }

        public static TDestination[] MapToArray<TSource, TDestination>(
            this IMapper<TSource, TDestination> mapper,
            IEnumerable<TSource> sources)
        {
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources), NullSourcesError);
            }

            return sources.Select(mapper.Map).ToArray();
        }
    }
}