using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobiAPI.Json
{
    public class GroupCan
    {
        public int add_members { get; set; }
        public int invite { get; set; }
        public int join { get; set; }
        public int kick { get; set; }
        public int part { get; set; }
        public int peek { get; set; }
        public int post_chat { get; set; }
        public int remove { get; set; }
        public int shout { get; set; }
        public int update_description { get; set; }
        public int update_icon { get; set; }
        public int update_name { get; set; }
        public int update_restriction { get; set; }
        public int update_wallpaper { get; set; }
    }
}
