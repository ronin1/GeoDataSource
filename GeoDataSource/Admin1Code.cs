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
    public class Admin1Code
    {
        [ProtoMember(1)]
        public string Code { get; set; }

        [ProtoMember(2)]
        public string Name { get; set; }

        public string CountryId { get; set; }
        public string GeoNameId { get; set; }
        public string ASCIIName { get; set; }
    }
}
