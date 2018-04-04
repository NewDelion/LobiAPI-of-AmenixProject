using RestSharp.Deserializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobiAPI.Json
{
    public class UserMinimal
    {
        [DeserializeAs(Name = "default")]
        public int Default { get; set; }

        [DeserializeAs(Name = "uid")]
        public string UserId { get; set; }

        [DeserializeAs(Name = "name")]
        public string Name { get; set; }

        [DeserializeAs(Name = "description")]
        public string Description { get; set; }

        [DeserializeAs(Name = "icon")]
        public string Icon { get; set; }

        [DeserializeAs(Name = "cover")]
        public string Cover { get; set; }
    }

    public class UserMinimalWithPremium : UserMinimal
    {
        [DeserializeAs(Name = "premium")]
        public int PremiumRaw { get; set; }
        
        public bool Premium => PremiumRaw != 0;
    }

    public class UserMinimalWithToken : UserMinimal
    {
        [DeserializeAs(Name = "token")]
        public string Token { get; set; }
    }

    public class UserInfo : UserMinimalWithPremium
    {
        [DeserializeAs(Name = "contacts_count")]
        public int ContactsCount { get; set; }

        [DeserializeAs(Name = "followers_count")]
        public int FollowersCount { get; set; }

        [DeserializeAs(Name = "public_groups_count")]
        public int PublicGroupsCount { get; set; }

        [DeserializeAs(Name = "is_blocked")]
        public int IsBlockedRaw { get; set; }
        
        public bool IsBlocked => IsBlockedRaw != 0;
    }

    public class Users : Cursorable
    {
        [DeserializeAs(Name = "users")]
        public List<UserMinimal> UserList { get; set; }
    }
}
