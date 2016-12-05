using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using LobiAPI.Json;
using LobiAPI.Utils;

namespace LobiAPI
{
    public class BasicAPI
    {
        private const string UserAgent = "LobiAPI-of-AmenixProject";
        private string DeviceUUID = "";
        private string Token = "";

        private async Task<string> GetSpell(string mail, string password)
        {
            using (HttpClientHandler handler = new HttpClientHandler { CookieContainer = new CookieContainer() })
            using (HttpClient client = new HttpClient(handler))
            {
                CookieContainer cookie = new CookieContainer();
                client.DefaultRequestHeaders.Host = "lobi.co";
                client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
                client.DefaultRequestHeaders.Add("Accept-Language", "ja-JP,en-US;q=0.8");
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 5.1; Google Nexus 10 - 5.1.0 - API 22 - 2560x1600 Build/LMY47D) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/39.0.0.0 Safari/537.36 Lobi/8.10.3");
                client.DefaultRequestHeaders.Add("X-Requested-With", "com.kayac.nakamap");
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                handler.AllowAutoRedirect = false;

                var res1 = await client.GetAsync("https://lobi.co/inapp/signin/password?webview=1");//Cookie & csrf_token 
                string source1 = await res1.Content.ReadAsStringAsync();
                string csrf_token = Pattern.get_string(source1, Pattern.csrf_token, "\"");
                client.DefaultRequestHeaders.Add("Referer", "https://lobi.co/inapp/signin/password?webview=1");
                client.DefaultRequestHeaders.Add("Origin", "https://lobi.co");
                FormUrlEncodedContent post_data = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "csrf_token", csrf_token },
                    { "email", mail },
                    { "password", password }
                });
                var res2 = await client.PostAsync("https://lobi.co/inapp/signin/password", post_data);//spell
                string key = "nakamapbridge://signin?spell=";
                if (res2.Headers.Location != null && res2.Headers.Location.OriginalString.IndexOf(key) == 0 && res2.Headers.Location.OriginalString.Length > key.Length)
                    return res2.Headers.Location.OriginalString.Substring(key.Length);
            }
            return "";
        }
        private async Task<string> GetToken(string device_uuid, string spell)
        {
            HMACSHA1 localMac = new HMACSHA1(Encoding.ASCII.GetBytes("db6db1788023ce4703eecf6aa33f5fcde35a458c"));
            string sig = "";
            using (System.IO.MemoryStream stream = new System.IO.MemoryStream(Encoding.ASCII.GetBytes(spell)))
                sig = Convert.ToBase64String(localMac.ComputeHash(stream));
            using (HttpClientHandler handler = new HttpClientHandler())
            using (HttpClient client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                client.DefaultRequestHeaders.Add("Host", "api.lobi.co");
                handler.AutomaticDecompression = DecompressionMethods.GZip;
                FormUrlEncodedContent post_data = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "device_uuid", device_uuid },
                    { "sig", sig },
                    { "spell", spell },
                    { "lang", "ja" }
                });
                var response = await client.PostAsync("https://api.lobi.co/1/signin_confirmation", post_data);
                string source = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<User>(source).token;
            }
        }

        public async Task<bool> Login(string mail, string password)
        {
            string spell = await GetSpell(mail, password);
            if (spell == null || spell == "")
                return false;
            DeviceUUID = Guid.NewGuid().ToString();
            Token = await GetToken(DeviceUUID, spell);
            return Token != null && (Token ?? "").Length > 0;
        }

        public async Task<User> GetMe()
        {
            return await GET<User>(1, "me");
        }
        
        /// <summary>
        /// フォロー取得
        /// </summary>
        public async Task<Contacts> GetContacts()
        {
            return await GET<Contacts>(1, "me/contacts");
        }
        /// <summary>
        /// 指定したユーザのフォロー取得
        /// </summary>
        public async Task<List<User>> GetContacts(string user_id)
        {
            List<User> result = new List<User>();
            Dictionary<string, string> data = new Dictionary<string, string>();
            while (true)
            {
                var res = await GET<Contacts>(1, string.Format("user/{0}/contacts", user_id), data);
                if (res == null || res.users == null || res.users.Length == 0)
                    break;
                result.AddRange(res.users);
                if (res.next_cursor == "-1" || res.next_cursor == "0")
                    break;
                if (data.ContainsKey("cursor"))
                    data["cursor"] = res.next_cursor;
                else
                    data.Add("cursor", res.next_cursor);
            }
            return result;
        }

        /// <summary>
        /// フォロワー取得
        /// </summary>
        public async Task<Followers> GetFollowers()
        {
            return await GET<Followers>(1, "me/followers");
        }
        /// <summary>
        /// 指定したユーザのフォロワー取得
        /// </summary>
        public async Task<List<User>> GetFollowers(string user_id)
        {
            List<User> result = new List<User>();
            Dictionary<string, string> data = new Dictionary<string, string>();
            while (true)
            {
                var res = await GET<Contacts>(1, string.Format("user/{0}/followers", user_id), data);
                if (res == null || res.users == null || res.users.Length == 0)
                    break;
                result.AddRange(res.users);
                if (res.next_cursor == "-1" || res.next_cursor == "0")
                    break;
                if (data.ContainsKey("cursor"))
                    data["cursor"] = res.next_cursor;
                else
                    data.Add("cursor", res.next_cursor);
            }
            return result;
        }

        public async Task<User> GetUser(string user_id)
        {
            return await GET<User>(1, string.Format("user/{0}", user_id));
        }

        public async Task<List<Group>> GetPublicGroupAll()
        {
            List<Group> result = new List<Group>();
            int page = 1;
            while (true)
            {
                var res = await GET<Groups>(1, "public_groups", new Dictionary<string, string>
                {
                    { "with_archived", "true" },
                    { "count", "1000" },
                    { "page", page++.ToString() }
                });
                if (res == null || res.items == null || res.items.Length == 0)
                    break;
                result.AddRange(res.items);
                if (res.items.Length < 1000)
                    break;
            }
            return result.ToList();
        }
        public async Task<List<Group>> GetPublicGroupAll(string user_id)
        {
            List<Group> result = new List<Group>();
            Dictionary<string, string> data = new Dictionary<string, string> { { "with_archived", "true" } };
            while (true)
            {
                var res = await GET<VisibleGroups>(1, string.Format("user/{0}/visible_groups", user_id), data);
                if (res == null || res.public_groups == null || res.public_groups.Length == 0)
                    break;
                result.AddRange(res.public_groups);
                if (res.next_cursor == "-1" || res.next_cursor == "0")
                    break;
                if (data.ContainsKey("cursor"))
                    data["cursor"] = res.next_cursor;
                else
                    data.Add("cursor", res.next_cursor);
            }
            return result;
        }
        public async Task<List<Group>> GetPublicGroup(int page, int count = 1000)
        {
            var res = await GET<Groups>(1, "public_groups", new Dictionary<string, string>
            {
                { "with_archived", "true" },
                { "count", count.ToString() },
                { "page", page.ToString() }
            });
            if (res == null || res.items == null)
                return new List<Group>();
            return res.items.ToList();
        }

        public async Task<List<Group>> GetPrivateGroupAll()
        {
            List<Group> result = new List<Group>();
            int page = 1;
            while (true)
            {
                var res = await GET<Groups>(3, "groups", new Dictionary<string, string>
                {
                    { "with_archived", "true" },
                    { "count", "1000" },
                    { "page", page++.ToString() }
                });
                if (res == null || res.items == null || res.items.Length == 0)
                    break;
                result.AddRange(res.items);
                if (res.items.Length < 1000)
                    break;
            }
            return result.ToList();
        }
        public async Task<List<Group>> GetPrivateGroup(int page, int count = 1000)
        {
            var res = await GET<Groups>(3, "groups", new Dictionary<string, string>
            {
                { "with_archived", "true" },
                { "count", count.ToString() },
                { "page", page.ToString() }
            });
            if (res == null || res.items == null)
                return new List<Group>();
            return res.items.ToList();
        }

        public async Task<Group> GetGroup(string group_id)
        {
            return await GET<Group>(2, string.Format("group/{0}", group_id), new Dictionary<string, string>
            {
                { "members_count", "1" },
                { "fields", "subleaders" }
            });
        }
        public async Task<User> GetGroupLeader(string group_id)
        {
            return (await GET<Members>(2, string.Format("group/{0}/members", group_id), new Dictionary<string, string>
            {
                { "members_count", "1" },
                { "fields", "owner" }
            })).owner;
        }
        public async Task<List<User>> GetGroupSubleaders(string group_id)
        {
            return ((await GET<Members>(2, string.Format("group/{0}/members", group_id), new Dictionary<string, string>
            {
                { "members_count", "1" },
                { "fields", "subleaders" }
            })).subleaders ?? new User[0]).ToList();
        }
        public async Task<List<User>> GetGroupMembersAll(string group_id)
        {
            List<User> result = new List<User>();
            Dictionary<string, string> data = new Dictionary<string, string> { { "members_count", "1000" } };
            while (true)
            {
                var res = await GET<Members>(1, string.Format("group/{0}/members", group_id), data);
                if (res == null || res.members == null || res.members.Length == 0)
                    break;
                result.AddRange(res.members);
                if (res.next_cursor == "0" || res.next_cursor == "-1")
                    break;
                if (data.ContainsKey("cursor"))
                    data["cursor"] = res.next_cursor;
                else
                    data.Add("cursor", res.next_cursor);
            }
            return result;
        }

        public async Task<List<Chat>> GetThreads(string group_id, int count = 20, string older_than = null, string newer_than = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string> { { "count", count.ToString() } };
            if (older_than != null && older_than != "")
                data.Add("older_than", older_than);
            if (newer_than != null && newer_than != "")
                data.Add("newer_than", newer_than);
            return await GET<List<Chat>>(2, string.Format("group/{0}/chats", group_id), data);
        }

        public async Task<Replies> GetRepliesAll(string group_id, string chat_id)
        {
            return await GET<Replies>(1, string.Format("group/{0}/chats/replies", group_id), new Dictionary<string, string> { { "to", chat_id } });
        }

        public async Task<List<PokeUserItem>> GetPokesAll(string group_id, string chat_id)
        {
            List<PokeUserItem> result = new List<PokeUserItem>();
            Dictionary<string, string> data = new Dictionary<string, string> { { "id", chat_id } };
            while (true)
            {
                var res = await GET<Pokes>(1, string.Format("group/{0}/chats/pokes", group_id), data);
                if (res == null || res.users == null || res.users.Length == 0)
                    break;
                result.AddRange(res.users);
                if (res.next_cursor == "0" || res.next_cursor == "-1")
                    break;
                if (data.ContainsKey("cursor"))
                    data["cursor"] = res.next_cursor;
                else
                    data.Add("cursor", res.next_cursor);
            }
            return result;
        }

        public async Task<Notifications> GetNotifications(int count = 20, string cursor = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string> { { "count", count.ToString() } };
            if (cursor != null && cursor != "")
                data.Add("cursor", cursor);
            return await GET<Notifications>(2, "info/notifications", data);
        }



        private async Task<T> GET<T>(int version, string request_url, Dictionary<string, string> queries = null)
        {
            using (HttpClientHandler handler = new HttpClientHandler())
            using (HttpClient client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                client.DefaultRequestHeaders.Add("Host", "api.lobi.co");
                handler.AutomaticDecompression = DecompressionMethods.GZip;
                string url = string.Format("https://api.lobi.co/{0}/{1}?platform=android&lang=ja&token={2}{3}", version, request_url, Token, queries == null ? "" : string.Join("", queries.Select(d => string.Format("&{0}={1}", WebUtility.UrlEncode(d.Key), WebUtility.UrlEncode(d.Value)))));
                var res = await client.GetAsync(url);
                if (res.StatusCode != HttpStatusCode.OK)
                    throw new RequestAPIException(new ErrorObject(res));
                string result = await res.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(result);
            }
        }

        private async Task<T> POST<T>(int version, string request_url, HttpContent post_data)
        {
            using (HttpClientHandler handler = new HttpClientHandler())
            using(HttpClient client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                client.DefaultRequestHeaders.Add("Host", "api.lobi.co");
                handler.AutomaticDecompression = DecompressionMethods.GZip;
                string url = string.Format("https://api.lobi.co/{0}/{1}", version, request_url);
                var res = await client.PostAsync(url, post_data);
                if (res.StatusCode != HttpStatusCode.OK)
                    throw new RequestAPIException(new ErrorObject(res));
                string result = await res.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(result);
            }
        }

        private class Pattern
        {
            public static string csrf_token = "<input type=\"hidden\" name=\"csrf_token\" value=\"";
            public static string get_string(string source, string pattern, string end_pattern)
            {
                int start = source.IndexOf(pattern) + pattern.Length;
                int end = source.IndexOf(end_pattern, start + 1);
                return source.Substring(start, end - start);
            }
        }
    }
}
