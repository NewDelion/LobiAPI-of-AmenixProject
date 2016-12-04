using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobiAPI.Json
{
    public class Pokes
    {
        public string next_cursor { get; set; }
        public PokeUserItem[] users { get; set; }
    }

    public class PokeUserItem
    {
        public long created_date { get; set; }
        public string type { get; set; }
        public User user { get; set; }
    }
}
