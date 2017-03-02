using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobiAPI.Json
{
    public abstract class Cursorable
    {
        public string next_cursor { get; set; }
    }
}
