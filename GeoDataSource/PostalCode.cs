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
    public class PostalCode
    {
        public Country Country { get; set; }

        /// <summary>
        /// Actual zip or postal code
        /// </summary>
        [ProtoMember(1)]
        public string Code { get; set; }

        /// <summary>
        /// Place name
        /// </summary>
        [ProtoMember(2)]
        public string Name { get; set; }

        [ProtoMember(3)]
        public Admin1Code Admin1 { get; set; }
        [ProtoMember(4)]
        public Admin2Code Admin2 { get; set; }
        [ProtoMember(5)]
        public Admin3Code Amdin3 { get; set; }

        [ProtoMember(6)]
        public decimal Latitude { get; set; }
        [ProtoMember(7)]
        public decimal Longitude { get; set; }

        /// <summary>
        /// GPS Accuracy
        /// </summary>
        [ProtoMember(8)]
        public GPSAccuracy Accuracy { get; set; }
    }
}
