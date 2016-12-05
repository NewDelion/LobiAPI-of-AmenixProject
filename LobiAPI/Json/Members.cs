using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobiAPI.Json
{
    public class Members
    {
        public User[] members { get; set; }
        public User owner { get; set; }
        public User[] subleaders { get; set; }
        public string next_cursor { get; set; }
    }
}
