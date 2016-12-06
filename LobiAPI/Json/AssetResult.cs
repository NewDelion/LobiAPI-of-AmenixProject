using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobiAPI.Json
{
    public class AssetResult
    {
        public string id { get; set; }
        public string type { get; set; }
        public int? order { get; set; }
        public string url { get; set; }
    }
}
