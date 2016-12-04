using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobiAPI.Json
{
    public class Notifications
    {
        public string last_cursor { get; set; }
        public Notification[] notifications { get; set; }
    }
}
