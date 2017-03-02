using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobiAPI.Json
{
    public class Chat
    {
        public Asset[] assets { get; set; }
        public int? assets_expired { get; set; }
        public int? booed { get; set; }
        public int? bookmarks_count { get; set; }
        public int? boos_count { get; set; }
        public long? created_date { get; set; }
        public long? edited_date { get; set; }
        public string id { get; set; }
        public string image { get; set; }
        public int? image_height { get; set; }
        public int? image_width { get; set; }
        public string image_type { get; set; }
        public int? is_group_bookmarked { get; set; }
        public int? is_me_bookmarked { get; set; }
        public int? liked { get; set; }
        public int? likes_count { get; set; }
        public long? max_edit_limit_date { get; set; }
        public string message { get; set; }
        public Reply replies { get; set; }
        public string reply_to { get; set; }
        public string stamp_id { get; set; }
        public string type { get; set; }
        public string[] urls { get; set; }
        public UserMinimal user { get; set; }
    }

    public class Reply
    {
        public Chat[] chats { get; set; }
        public Chat to { get; set; }
        public int? count { get; set; }
    }

    public class Pokes : Cursorable
    {
        public PokeUserItem[] users { get; set; }
    }

    public class PokeUserItem
    {
        public long? created_date { get; set; }
        public string type { get; set; }
        public UserMinimal user { get; set; }
    }

    public class Members : Cursorable
    {
        public UserSmall[] members { get; set; }
        public UserSmall owner { get; set; }
        public UserSmall[] subleaders { get; set; }
    }

    public class Asset
    {
        public string url { get; set; }
        public string type { get; set; }
        public string id { get; set; }
        public string raw_url { get; set; }
        public int? order { get; set; }
    }

    public class Bookmarks : Cursorable
    {
        public Chat[] data { get; set; }
    }
}
