using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobiAPI.Json
{
    public class Group
    {
        public long? created_date { get; set; }
        public string description { get; set; }
        public GroupBookmarkInfo group_bookmark_info { get; set; }
        public string icon { get; set; }
        public User invited_by { get; set; }
        public int? is_archived { get; set; }
        public int? is_authorized { get; set; }
        public int? is_notice { get; set; }
        public int? is_official { get; set; }
        public int? is_online { get; set; }
        public int? is_public { get; set; }
        public GroupMe me { get; set; }
        public User[] members { get; set; }
        public int? members_count { get; set; }
        public int? members_next_cursor { get; set; }
        public long? last_chat_at { get; set; }
        public string name { get; set; }
        public object[] needs_to_join { get; set; }
        public string now { get; set; }
        public User owner { get; set; }
        public User[] subleaders { get; set; }
        public int? online_users { get; set; }
        public int? push_enabled { get; set; }
        public string stream_host { get; set; }
        public long? total_users { get; set; }
        public string type { get; set; }
        public string uid { get; set; }
        public string wallpaper { get; set; }
        public object game { get; set; }//いらない情報だからオブジェクトにしてるけどもしかしたらパースできないかも
        public GroupCan can { get; set; }
        public Chat[] chats { get; set; }
    }

    public class GroupBookmarkInfo
    {
        public int? can_request { get; set; }
        public int? group_bookmark_count { get; set; }
        public int? has_bookmark_count { get; set; }
        public int? request_count { get; set; }
    }

    public class GroupMe
    {
        public int? can_extract { get; set; }
        public int? is_online { get; set; }
    }
}
