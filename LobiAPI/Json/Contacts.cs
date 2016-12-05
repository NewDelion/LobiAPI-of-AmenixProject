using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobiAPI.Json
{
    public class Contacts
    {
        public string next_cursor { get; set; }
        public int? visibility { get; set; }
        public User[] users { get; set; }
    }
}
