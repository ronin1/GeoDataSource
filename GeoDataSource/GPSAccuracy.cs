using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoDataSource
{
    [Serializable]
    public enum GPSAccuracy : int
    {
        Unknown = 0,
        Estimated = 1,
        NotGreat = 2,
        CityLevel = 3,
        FifteenMeters = 4,
        OneMeter = 5,
        Centroid = 6,
    }
}
