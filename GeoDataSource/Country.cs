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
    public class Country
    {
        [ProtoMember(1)]
        public string ISONumeric { get; set; }
        [ProtoMember(2)]
        public string ISOAlpha2 { get; set; }
        [ProtoMember(3)]
        public string ISOAlpha3 { get; set; }        
        [ProtoMember(4)]
        public string Name { get; set; }
        [ProtoMember(5)]
        public string FipsCode { get; set; }
        [ProtoMember(6)]
        public string Capital { get; set; }
        [ProtoMember(7)]
        public string Area { get; set; }
        [ProtoMember(8)]
        public string Population { get; set; }
        [ProtoMember(9)]
        public string ContinentId { get; set; }
        [ProtoMember(10)]
        public string Continent { get; set; }
        [ProtoMember(11)]
        public string tld { get; set; }
        [ProtoMember(12)]
        public string CurrencyCode { get; set; }
        [ProtoMember(13)]
        public string CurrencyName { get; set; }
        [ProtoMember(14)]
        public string PhonePrefix { get; set; }
        [ProtoMember(15)]
        public string PostalCodeFormat { get; set; }
        [ProtoMember(16)]
        public string PostalCodeRegularExpression { get; set; }
        [ProtoMember(17)]
        public string Languages { get; set; }
        [ProtoMember(18)]
        public string EquivalentFipsCode { get; set; }
        [ProtoMember(19)]
        public string Neighbours { get; set; }

        public int GeoNameId { get; set; }
        public IEnumerable<PhoneInformation> PhoneInformation { get; set; }
    }
}