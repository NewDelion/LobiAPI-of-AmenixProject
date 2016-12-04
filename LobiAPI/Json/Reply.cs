using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobiAPI.Json
{
    public class Reply
    {
        public Chat[] chats { get; set; }
        public int? count { get; set; }
    }
}
