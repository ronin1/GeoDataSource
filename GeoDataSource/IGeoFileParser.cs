using System.Collections.Generic;

namespace GeoDataSource
{
    public interface IGeoFileParser<T>
    {
        ICollection<T> ParseFile();
    }
}