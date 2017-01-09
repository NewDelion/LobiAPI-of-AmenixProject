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
using System.IO;
using System.Net.Http.Headers;

namespace LobiAPI
{
    public class BasicAPI
    {
        private readonly string UserAgent = "LobiAPI-of-AmenixProject";
        private readonly string platform = "android";
        private string DeviceUUID = "";
        public string Token { get; set; }

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

                string source1 = null;
                using (var res1 = await client.GetAsync("https://lobi.co/inapp/signin/password?webview=1")) //Cookie & csrf_token 
                    source1 = await res1.Content.ReadAsStringAsync();
                string csrf_token = Pattern.get_string(source1, Pattern.csrf_token, "\"");
                client.DefaultRequestHeaders.Add("Referer", "https://lobi.co/inapp/signin/password?webview=1");
                client.DefaultRequestHeaders.Add("Origin", "https://lobi.co");
                FormUrlEncodedContent post_data = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "csrf_token", csrf_token },
                    { "email", mail },
                    { "password", password }
                });
                using (var res2 = await client.PostAsync("https://lobi.co/inapp/signin/password", post_data))//spell
                {
                    string key = "nakamapbridge://signin?spell=";
                    if (res2.Headers.Location != null && res2.Headers.Location.OriginalString.IndexOf(key) == 0 && res2.Headers.Location.OriginalString.Length > key.Length)
                        return res2.Headers.Location.OriginalString.Substring(key.Length);
                }
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
                using (var response = await client.PostAsync("https://api.lobi.co/1/signin_confirmation", post_data))
                {
                    string source = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<User>(source).token;
                }
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

        public async Task<Me> GetMe()
        {
            return await GET<Me>(1, "me");
        }
        
        /// <summary>
        /// フォロー取得
        /// </summary>
        public async Task<Contacts> GetContacts()
        {
            return await GET<Contacts>(3, "me/contacts");
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
                if (res.next_cursor == null || res.next_cursor == "-1" || res.next_cursor == "0")
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
            return await GET<Followers>(2, "me/followers");
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
                if (res.next_cursor == null || res.next_cursor == "-1" || res.next_cursor == "0")
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
            return await GET<User>(1, string.Format("user/{0}", user_id), new Dictionary<string, string> { { "fields", "is_blocked" } });
        }
        public async Task<List<User>> GetBlockingUsersAll()
        {
            List<User> result = new List<User>();
            Dictionary<string, string> data = new Dictionary<string, string>();
            while (true)
            {
                var res = await GET<BlockingUsersResult>(2, "me/blocking_users", data);
                if (res == null || res.users == null || res.users.Length == 0)
                    break;
                result.AddRange(res.users);
                if (res.next_cursor == null || res.next_cursor == "-1" || res.next_cursor == "0")
                    break;
                if (data.ContainsKey("cursor"))
                    data["cursor"] = res.next_cursor;
                else
                    data.Add("cursor", res.next_cursor);
            }
            return result;
        }

        /// <summary>
        /// 招待されているグループ
        /// </summary>
        public async Task<Groups> GetInvited()
        {
            return await GET<Groups>(1, string.Format("groups/invited"));
        }

        public async Task<List<Group>> GetPublicGroupAll()
        {
            List<Group> result = new List<Group>();
            int page = 1;
            while (true)
            {
                var res = await GET<Groups[]>(1, "public_groups", new Dictionary<string, string>
                {
                    { "with_archived", "true" },
                    { "count", "1000" },
                    { "page", page++.ToString() }
                });
                if (res == null || res.Length == 0 || res[0] == null || res[0].items == null || res[0].items.Length == 0)
                    break;
                result.AddRange(res[0].items);
                if (res[0].items.Length < 1000)
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
                if (res.next_cursor == null || res.next_cursor == "-1" || res.next_cursor == "0")
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
            var res = await GET<Groups[]>(1, "public_groups", new Dictionary<string, string>
            {
                { "with_archived", "true" },
                { "count", count.ToString() },
                { "page", page.ToString() }
            });
            if (res == null || res.Length == 0 || res[0] == null || res[0].items == null)
                return new List<Group>();
            return res[0].items.ToList();
        }

        public async Task<List<Group>> GetPrivateGroupAll()
        {
            List<Group> result = new List<Group>();
            int page = 1;
            while (true)
            {
                var res = await GET<Groups[]>(3, "groups", new Dictionary<string, string>
                {
                    { "with_archived", "true" },
                    { "count", "1000" },
                    { "page", page++.ToString() }
                });
                if (res == null || res.Length == 0 || res[0] == null || res[0].items == null || res[0].items.Length == 0)
                    break;
                result.AddRange(res[0].items);
                if (res[0].items.Length < 1000)
                    break;
            }
            return result.ToList();
        }
        public async Task<List<Group>> GetPrivateGroup(int page, int count = 1000)
        {
            var res = await GET<Groups[]>(3, "groups", new Dictionary<string, string>
            {
                { "with_archived", "true" },
                { "count", count.ToString() },
                { "page", page.ToString() }
            });
            if (res == null || res.Length == 0 || res[0] == null || res[0].items == null)
                return new List<Group>();
            return res[0].items.ToList();
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
                if (res.next_cursor == null || res.next_cursor == "0" || res.next_cursor == "-1")
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
                if (res.next_cursor == null || res.next_cursor == "0" || res.next_cursor == "-1")
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
                data.Add("last_cursor", cursor);
            return await GET<Notifications>(2, "info/notifications", data);
        }

        public async Task<RequestResult> Like(string group_id, string chat_id)
        {
            return await POST<RequestResult>(1, string.Format("group/{0}/chats/like", group_id), new Dictionary<string, string> { { "id", chat_id } });
        }
        public async Task<RequestResult> UnLike(string group_id, string chat_id)
        {
            return await POST<RequestResult>(1, string.Format("group/{0}/chats/unlike", group_id), new Dictionary<string, string> { { "id", chat_id } });
        }
        public async Task<RequestResult> Boo(string group_id, string chat_id)
        {
            return await POST<RequestResult>(1, string.Format("group/{0}/chats/boo", group_id), new Dictionary<string, string> { { "id", chat_id } });
        }
        public async Task<RequestResult> UnBoo(string group_id, string chat_id)
        {
            return await POST<RequestResult>(1, string.Format("group/{0}/chats/unboo", group_id), new Dictionary<string, string> { { "id", chat_id } });
        }

        public async Task<RequestResult> Follow(string user_id)
        {
            return await POST<RequestResult>(1, "me/contacts", new Dictionary<string, string> { { "users", user_id } });
        }
        public async Task<RequestResult> UnFollow(string user_id)
        {
            return await POST<RequestResult>(1, "me/contacts/remove", new Dictionary<string, string> { { "users", user_id } });
        }

        public async Task<List<AssetResult>> Assets(List<string> files)
        {
            if (files == null || files.Count == 0)
                throw new ArgumentNullException("ファイルを1枚以上指定してください");
            foreach (string file in files)
                if (!File.Exists(file))
                    throw new FileNotFoundException();
            List<AssetResult> result = new List<AssetResult>();
            int order = files.Count > 1 ? 0 : -1;
            foreach (string file in files)
            {
                using (HttpClientHandler handler = new HttpClientHandler())
                using (HttpClient client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
                    client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                    client.DefaultRequestHeaders.Add("Host", "api.lobi.co");
                    handler.AutomaticDecompression = DecompressionMethods.GZip;
                    MultipartFormDataContent post_data = new MultipartFormDataContent();
                    post_data.Add(new StringContent("ja"), "\"lang\"");
                    post_data.Add(new StringContent(order == -1 ? "" : (order++).ToString()), "\"order\"");
                    post_data.Add(new StringContent(Token), "\"token\"");
                    var asset = new StreamContent(File.OpenRead(file));
                    string extension = Path.GetExtension(file).ToLower();
                    string content_type = "";
                    if (extension == ".jpeg" || extension == ".jpg")
                        content_type = "image/jpeg";
                    else if (extension == ".png")
                        content_type = "image/png";
                    else if (extension == ".webp")
                        content_type = "image/webp";
                    else
                        throw new Exception("この画像フォーマットは対応していません");
                    asset.Headers.ContentType = new MediaTypeHeaderValue(content_type);
                    post_data.Add(asset, "\"asset\"", "\"" + Path.GetFileName(file) + "\"");
                    using (var res = await client.PostAsync("https://api.lobi.co/1/assets", post_data))
                    {
                        if (res.StatusCode != HttpStatusCode.OK)
                            throw new RequestAPIException(new ErrorObject(res));
                        string content = await res.Content.ReadAsStringAsync();
                        result.Add(JsonConvert.DeserializeObject<AssetResult>(content));
                    }
                }
            }
            return result;
        }
        public async Task<Chat> Chat(string group_id, string message, bool shout = false)
        {
            if (message == null || message == "")
                throw new ArgumentNullException("メッセージが指定されていません");
            return await POST<Chat>(1, string.Format("group/{0}/chats", group_id), new Dictionary<string, string>
            {
                { "type", shout ? "shout" : "normal" },
                { "message", message }
            });
        }
        public async Task<Chat> Chat(string group_id, string message, List<string> images, bool shout = false)
        {
            if (message == null || message == "")
                throw new ArgumentNullException("メッセージが指定されていません");
            var assets = await Assets(images);
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "type", shout ? "shout" : "normal" },
                { "message", message }
            };
            if (assets.Count > 0)
                data.Add("assets", string.Join(",", assets.Select(d => d.id + ":image")));
            return await POST<Chat>(1, string.Format("group/{0}/chats", group_id), data);
        }
        public async Task<Chat> Chat(string group_id, string message, string reply_to)
        {
            if (message == null || message == "")
                throw new ArgumentNullException("メッセージが指定されていません");
            if (reply_to == null || reply_to == "")
                throw new ArgumentNullException("返信元が指定されていません");
            return await POST<Chat>(1, string.Format("group/{0}/chats", group_id), new Dictionary<string, string>
            {
                { "type", "normal" },
                { "reply_to", reply_to },
                { "message", message }
            });
        }
        public async Task<Chat> Chat(string group_id, string message, List<string> images, string reply_to)
        {
            if (message == null || message == "")
                throw new ArgumentNullException("メッセージが指定されていません");
            if (reply_to == null || reply_to == "")
                throw new ArgumentNullException("返信元が指定されていません");
            var assets = await Assets(images);
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "type", "normal" },
                { "reply_to", reply_to },
                { "message", message }
            };
            if (assets.Count > 0)
                data.Add("assets", string.Join(",", assets.Select(d => d.id + ":image")));
            return await POST<Chat>(1, string.Format("group/{0}/chats", group_id), data);
        }
        public async Task<RequestResult> RemoveChat(string group_id, string chat_id)
        {
            return await POST<RequestResult>(1, string.Format("group/{0}/chats/remove", group_id), new Dictionary<string, string> { { "id", chat_id } });
        }

        public async Task<Group> Join(string group_id)
        {
            return await POST<Group>(1, string.Format("group/{0}/join", group_id), new Dictionary<string, string>());
        }
        /// <summary>
        /// 招待を拒否
        /// </summary>
        public async Task<RequestResult> RefuseInvitation(string group_id)
        {
            return await POST<RequestResult>(1, string.Format("group/{0}/refuse_invitation", group_id), new Dictionary<string, string>());
        }
        public async Task<RequestResult> Kick(string group_id, string user_id)
        {
            return await POST<RequestResult>(1, string.Format("group/{0}/kick", group_id), new Dictionary<string, string> { { "target_user", user_id } });
        }
        public async Task<Group> LeaderTransfer(string group_id, string user_id)
        {
            return await POST<Group>(1, string.Format("group/{0}/transfer", group_id), new Dictionary<string, string> { { "target_user", user_id } });
        }
        public async Task<RequestResult> SetSubleader(string group_id, string user_id)
        {
            return await POST<RequestResult>(1, string.Format("group/{0}/subleaders", group_id), new Dictionary<string, string> { { "user", user_id } });
        }
        public async Task<RequestResult> RemoveSubleader(string group_id, string user_id)
        {
            return await POST<RequestResult>(1, string.Format("group/{0}/subleaders/remove", group_id), new Dictionary<string, string> { { "user", user_id } });
        }

        public async Task<RequestResult> Block(string user_id)
        {
            return await POST<RequestResult>(1, "me/blocking_users", new Dictionary<string, string> { { "users", user_id } });
        }
        public async Task<RequestResult> Unblock(string user_id)
        {
            return await POST<RequestResult>(1, "me/blocking_users/remove", new Dictionary<string, string> { { "users", user_id } });
        }

        private async Task<T> GET<T>(int version, string request_url, Dictionary<string, string> query = null)
        {
            using (HttpClientHandler handler = new HttpClientHandler())
            using (HttpClient client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                client.DefaultRequestHeaders.Add("Host", "api.lobi.co");
                handler.AutomaticDecompression = DecompressionMethods.GZip;
                string url = string.Format("https://api.lobi.co/{0}/{1}?platform={2}&lang=ja&token={3}{4}", version, request_url, platform, Token, query == null ? "" : string.Join("", query.Select(d => string.Format("&{0}={1}", WebUtility.UrlEncode(d.Key), WebUtility.UrlEncode(d.Value)))));
                using (var res = await client.GetAsync(url))
                {
                    if (res.StatusCode != HttpStatusCode.OK)
                        throw new RequestAPIException(new ErrorObject(res));
                    return JsonConvert.DeserializeObject<T>(await res.Content.ReadAsStringAsync());
                }
            }
        }
        private async Task<T> POST<T>(int version, string request_url, Dictionary<string, string> query)
        {
            if (query == null)
                throw new ArgumentNullException("queryがnullです");
            using (HttpClientHandler handler = new HttpClientHandler())
            using(HttpClient client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                client.DefaultRequestHeaders.Add("Host", "api.lobi.co");
                handler.AutomaticDecompression = DecompressionMethods.GZip;
                string url = string.Format("https://api.lobi.co/{0}/{1}", version, request_url);
                FormUrlEncodedContent post_data = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "lang", "ja" },
                    { "token", Token }
                }.Concat(query));
                using (var res = await client.PostAsync(url, post_data))
                {
                    if (res.StatusCode != HttpStatusCode.OK)
                        throw new RequestAPIException(new ErrorObject(res));
                    string result = await res.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<T>(result);
                }
            }
        }
        private async Task<string> POST_TEST(int version, string request_url, Dictionary<string, string> query)
        {
            if (query == null)
                throw new ArgumentNullException("queryがnullです");
            using (HttpClientHandler handler = new HttpClientHandler())
            using (HttpClient client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                client.DefaultRequestHeaders.Add("Host", "api.lobi.co");
                handler.AutomaticDecompression = DecompressionMethods.GZip;
                string url = string.Format("https://api.lobi.co/{0}/{1}", version, request_url);
                FormUrlEncodedContent post_data = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "lang", "ja" },
                    { "token", Token }
                }.Concat(query));
                using (var res = await client.PostAsync(url, post_data))
                {
                    if (res.StatusCode != HttpStatusCode.OK)
                        throw new RequestAPIException(new ErrorObject(res));
                    string result = await res.Content.ReadAsStringAsync();
                    return result;
                }
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
