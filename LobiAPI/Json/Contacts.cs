using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobiAPI.Json
{
    public class Contacts
    {
        public int? visibility { get; set; }
        public User[] users { get; set; }
    }
}
