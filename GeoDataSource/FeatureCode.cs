using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace GeoDataSource
{
    [ProtoContract(AsReferenceDefault = true)]
    [Serializable]
    public class FeatureCode
    {
        [ProtoMember(1)]
        public string Code { get; set; }
        [ProtoMember(2)]
        public string Class { get; set; }
        [ProtoMember(3)]
        public string Name { get; set; }
        //[ProtoMember(4)]
        public string Description { get; set; }
    }
}
