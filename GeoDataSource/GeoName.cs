using System;
using System.Collections.Generic;
using ProtoBuf;

namespace GeoDataSource
{
    [ProtoContract]
    [Serializable]
    public class GeoName
    {
        [ProtoMember(1)]
        public int GeoNameId { get; set; }
        [ProtoMember(2)]
        public string Name { get; set; }
        [ProtoMember(3)]
        public string AsciiName { get; set; }
        [ProtoMember(4)]
        public ICollection<string> AlternateNames { get; set; }
        [ProtoMember(5)]
        public decimal Latitude { get; set; }
        [ProtoMember(6)]
        public decimal Longitude { get; set; }
        [ProtoMember(7)]
        public string FeatureClass { get; set; }
        [ProtoMember(8)]
        public string FeatureCodeId { get; set; }
        [ProtoMember(9)]
        public string CountryCode { get; set; }
        [ProtoMember(10)]
        public string AlternateCountryCode { get; set; }
        [ProtoMember(11)]
        public string Admin1Code { get; set; }
        [ProtoMember(12)]
        public string Admin2Code { get; set; }
        [ProtoMember(13)]
        public string Admin3Code { get; set; }
        [ProtoMember(14)]
        public string Admin4Code { get; set; }
        [ProtoMember(15)]
        public long Population { get; set; }
        [ProtoMember(16)]
        public int Elevation { get; set; }
        [ProtoMember(17)]
        public string DigitalElevationModel { get; set; }
        [ProtoMember(18)]
        public string TimeZoneId { get; set; }
        [ProtoMember(19)]
        public DateTime LastModified { get; set; }
        [ProtoMember(20)]
        public string TwoLetterName { get; set; }

        [ProtoMember(21, AsReference = true)]
        public Country Country { get; set; }

        [ProtoMember(22, AsReference = true)]
        public TimeZone TimeZone { get; set; }
        public FeatureCode FeatureCode { get; set; }
    }
}
