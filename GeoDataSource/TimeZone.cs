using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace GeoDataSource
{
    [ProtoContract]
    [Serializable]
    public class TimeZone
    {
        [ProtoMember(1)]
        public string CountryCode { get; set; }
        [ProtoMember(2)]
        public string TimeZoneId { get; set; }
        [ProtoMember(3)]
        public double GMTOffSet { get; set; }
        [ProtoMember(4)]
        public double DSTOffSet { get; set; }
        [ProtoMember(5)]
        public double RawOffSet { get; set; }
    }
}
