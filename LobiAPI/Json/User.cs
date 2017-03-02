using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobiAPI.Json
{
    public class UserMinimal
    {
        public int? @default { get; set; }
        public string uid { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string icon { get; set; }
        public string cover { get; set; }
        public int? premium { get; set; }

        public string token { get; set; }
    }

    public class UserSmall : UserMinimal
    {
        public int? unread_count { get; set; }
        public long? last_chat_at { get; set; }

        public float? lat { get; set; }
        public float? lng { get; set; }
        public long? located_date { get; set; }
    }

    public class UserMidium : UserSmall //要チェック
    {
        public UserSmall[] uesrs { get; set; }
        public string users_next_cursor { get; set; }
    }

    public class User : UserMidium
    {
        public long? followed_date { get; set; }
        public long? following_date { get; set; }
        public int? followers_count { get; set; }
        public long? contacted_date { get; set; }
        public int? contacts_count { get; set; }

        public int? is_blocked { get; set; }

        public int? my_groups_count { get; set; }

        public MePublicGroupsItem[] public_groups { get; set; }
        public int? public_groups_count { get; set; }
        public string public_groups_next_cursor { get; set; }
    }

    public class MePublicGroupsItem
    {
        public int? visibility { get; set; }
        public Group group { get; set; }
    }

    public class Users : Cursorable
    {
        public User[] users { get; set; }
    }

    public class Contacts : Users
    {
        public int? visibility { get; set; }
    }

    public class Followers : Users
    {
        public int? visibility { get; set; }
    }
}
