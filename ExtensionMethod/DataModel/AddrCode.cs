using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtensionMethods.DataModel
{
    /// <summary>
    /// 座標資訊轉Code3統計區資訊
    /// </summary>
    public class AddrCode
    {
        public string displayFieldName { get; set; }
        public AddrCodeFieldaliases fieldAliases { get; set; }
        public string geometryType { get; set; }
        public AddrCodeSpatialreference spatialReference { get; set; }
        public AddrCodeField[] fields { get; set; }
        public AddrCodeFeature[] features { get; set; }
    }

    public class AddrCodeFieldaliases
    {
        public string COUN_ID { get; set; }
        public string COUN_NA { get; set; }
        public string TOWN_ID { get; set; }
        public string TOWN_NA { get; set; }
        public string CODE3 { get; set; }
        public string CODE2 { get; set; }
        public string CODE1 { get; set; }
    }

    public class AddrCodeSpatialreference
    {
        public int wkid { get; set; }
        public int latestWkid { get; set; }
    }

    public class AddrCodeField
    {
        public string name { get; set; }
        public string type { get; set; }
        public string alias { get; set; }
        public int length { get; set; }
    }

    public class AddrCodeFeature
    {
        public AddrCodeAttributes attributes { get; set; }
        public AddrCodeGeometry geometry { get; set; }
    }

    public class AddrCodeAttributes
    {
        public string COUN_ID { get; set; }
        public string COUN_NA { get; set; }
        public string TOWN_ID { get; set; }
        public string TOWN_NA { get; set; }
        public string CODE3 { get; set; }
        public string CODE2 { get; set; }
        public string CODE1 { get; set; }
    }

    public class AddrCodeGeometry
    {
        public float[][][] rings { get; set; }
    }


}
