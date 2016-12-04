using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobiAPI.Json
{
    public class Notification
    {
        public object[] click_hook { get; set; }
        public long created_date { get; set; }
        public object[] display { get; set; }
        public object[] display_hook { get; set; }
        public string icon { get; set; }
        public string id { get; set; }
        public string link { get; set; }
        public string message { get; set; }
        public string type { get; set; }
        public User user { get; set; }
        public NotificationTitle title { get; set; }
    }

    public class NotificationTitle
    {
        public NotificationTitleItem[] items { get; set; }
        public string template { get; set; }
    }

    public class NotificationTitleItem
    {
        public string label { get; set; }
        public string link { get; set; }
    }
}
