using RestSharp.Deserializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobiAPI.Json
{
    public abstract class Cursorable
    {
        [DeserializeAs(Name = "next_cursor")]
        public string NextCursor { get; set; }
    }
}
