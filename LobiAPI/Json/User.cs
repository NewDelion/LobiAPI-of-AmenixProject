using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobiAPI.Json
{
    public class User : UserMinimal
    {
        public int? @default { get; set; }
        public string lat { get; set; }
        public string lng { get; set; }
        public string located_date { get; set; }
        public string token { get; set; }
        public long? followed_date { get; set; }
        public long? following_date { get; set; }
        public int? is_blocked { get; set; }
    }
}
