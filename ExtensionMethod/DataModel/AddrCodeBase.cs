using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtensionMethods.DataModel
{
    /// <summary>
    /// 座標轉CodeBase統計區資訊
    /// </summary>
    public class AddrCodeBase
    {
        public string displayFieldName { get; set; }
        public AddrCodeBaseFieldaliases fieldAliases { get; set; }
        public AddrCodeBaseField[] fields { get; set; }
        public AddrCodeBaseFeature[] features { get; set; }
    }

    public class AddrCodeBaseFieldaliases
    {
        public string CODEBASE { get; set; }
    }

    public class AddrCodeBaseField
    {
        public string name { get; set; }
        public string type { get; set; }
        public string alias { get; set; }
        public int length { get; set; }
    }

    public class AddrCodeBaseFeature
    {
        public AddrCodeBaseAttributes attributes { get; set; }
    }

    public class AddrCodeBaseAttributes
    {
        public string CODEBASE { get; set; }
    }


}
