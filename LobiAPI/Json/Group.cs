using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobiAPI.Json
{
    public class GroupMinimal
    {
        public string uid { get; set; }
        public string name { get; set; }
    }

    public class GroupSmall : GroupMinimal
    {
        public long? created_date { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public GroupCan can { get; set; }
        public string stream_host { get; set; }

        public string icon { get; set; }
        public string wallpaper { get; set; }

        public int? is_archived { get; set; }
        public int? is_authorized { get; set; }
        public int? is_notice { get; set; }
        public int? is_official { get; set; }
        public int? is_online { get; set; }
        public int? is_public { get; set; }

        public long? last_chat_at { get; set; }
        public int? online_users { get; set; }
        public int? total_users { get; set; }

        public int? push_enabled { get; set; }
    }

    public class GroupInvited : GroupSmall
    {
        public UserMinimal invited_by { get; set; }
    }

    public class Group : GroupSmall
    {
        public Chat[] chats { get; set; }
        public UserSmall owner { get; set; }
        public UserSmall[] subleaders { get; set; }
        public object[] needs_to_join { get; set; }
        public string now { get; set; }
        public GroupMe me { get; set; }
        public GroupBookmarkInfo group_bookmark_info { get; set; }

        public UserSmall[] members { get; set; }
        public int? members_count { get; set; }
        public string members_next_cursor { get; set; }
    }

    public class Groups
    {
        public string title { get; set; }
        public Group[] items { get; set; }
    }

    public class VisibleGroups : Cursorable
    {
        public Group[] public_groups { get; set; }
    }

    public class GroupCan
    {
        public int? add_members { get; set; }
        public int? invite { get; set; }
        public int? join { get; set; }
        public int? kick { get; set; }
        public int? part { get; set; }
        public int? peek { get; set; }
        public int? post_chat { get; set; }
        public int? remove { get; set; }
        public int? shout { get; set; }
        public int? update_description { get; set; }
        public int? update_icon { get; set; }
        public int? update_name { get; set; }
        public int? update_restriction { get; set; }
        public int? update_wallpaper { get; set; }
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
