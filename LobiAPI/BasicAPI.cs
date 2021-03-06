﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        private string GetCsrfToken(string source)
        {
            var m = System.Text.RegularExpressions.Regex.Match(source, "name=\"csrf_token\" value=\"(?<csrf_token>[a-z0-9]+)\"", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!m.Success || !m.Groups["csrf_token"].Success || string.IsNullOrEmpty(m.Groups["csrf_token"].Value))
                return null;
            return m.Groups["csrf_token"].Value;
        }
        private async Task<string> GetSpell(string mail, string password)
        {
            using (HttpClientHandler handler = new HttpClientHandler { CookieContainer = new CookieContainer() })
            using (HttpClient client = new HttpClient(handler))
            {
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
                string csrf_token = GetCsrfToken(source1);
                if (string.IsNullOrEmpty(csrf_token))
                    return "";
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
                    if (res2.Headers.Location != null && res2.Headers.Location.OriginalString.StartsWith("nakamapbridge://signin?spell=") && res2.Headers.Location.OriginalString.Length > 29/*key length*/)
                        return res2.Headers.Location.OriginalString.Substring(29);
                }
            }
            return "";
        }
        private async Task<string> GetToken(string device_uuid, string spell)
        {
            string sig = "";
            using (HMACSHA1 localMac = new HMACSHA1(Encoding.ASCII.GetBytes("db6db1788023ce4703eecf6aa33f5fcde35a458c")))
            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(spell)))
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
            if (string.IsNullOrEmpty(spell))
                return false;
            DeviceUUID = Guid.NewGuid().ToString();
            Token = await GetToken(DeviceUUID, spell);
            return !string.IsNullOrEmpty(Token);
        }

        public Task<User> GetMe()
        {
            return GetMe(CancellationToken.None);
        }
        public async Task<User> GetMe(CancellationToken cancelToken)
        {
            return await GET<User>(1, "me", cancelToken);
        }
        
        /// <summary>
        /// フォロー取得
        /// </summary>
        public Task<Contacts> GetContacts()
        {
            return GetContacts(CancellationToken.None);
        }
        public async Task<Contacts> GetContacts(CancellationToken cancelToken)
        {
            return await GET<Contacts>(3, "me/contacts", cancelToken);
        }
        /// <summary>
        /// 指定したユーザのフォロー取得
        /// </summary>
        public Task<List<User>> GetContacts(string user_id)
        {
            return GetContacts(user_id, CancellationToken.None);
        }
        public async Task<List<User>> GetContacts(string user_id, CancellationToken cancelToken)
        {
            List<User> result = new List<User>();
            Dictionary<string, string> data = new Dictionary<string, string>();
            while (true)
            {
                var res = await GET<Contacts>(1, $"user/{user_id}/contacts", cancelToken, data);
                if (cancelToken.IsCancellationRequested)
                    return null;
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
            if (cancelToken.IsCancellationRequested)
                return null;
            return result;
        }

        /// <summary>
        /// フォロワー取得
        /// </summary>
        public Task<Followers> GetFollowers()
        {
            return GetFollowers(CancellationToken.None);
        }
        public async Task<Followers> GetFollowers(CancellationToken cancelToken)
        {
            return await GET<Followers>(2, "me/followers", cancelToken);
        }
        /// <summary>
        /// 指定したユーザのフォロワー取得
        /// </summary>
        public Task<List<User>> GetFollowers(string user_id)
        {
            return GetFollowers(user_id, CancellationToken.None);
        }
        public async Task<List<User>> GetFollowers(string user_id, CancellationToken cancelToken)
        {
            List<User> result = new List<User>();
            Dictionary<string, string> data = new Dictionary<string, string>();
            while (true)
            {
                var res = await GET<Contacts>(1, $"user/{user_id}/followers", cancelToken, data);
                if (cancelToken.IsCancellationRequested)
                    return null;
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
            if (cancelToken.IsCancellationRequested)
                return null;
            return result;
        }

        public Task<User> GetUser(string user_id)
        {
            return GetUser(user_id, CancellationToken.None);
        }
        public async Task<User> GetUser(string user_id, CancellationToken cancelToken)
        {
            return await GET<User>(1, $"user/{user_id}", cancelToken, new Dictionary<string, string> { { "fields", "is_blocked,public_groups_count" } });
        }
        public Task<List<User>> GetBlockingUsersAll()
        {
            return GetBlockingUsersAll(CancellationToken.None);
        }
        public async Task<List<User>> GetBlockingUsersAll(CancellationToken cancelToken)
        {
            List<User> result = new List<User>();
            Dictionary<string, string> data = new Dictionary<string, string>();
            while (true)
            {
                var res = await GET<Users>(2, "me/blocking_users", cancelToken, data);
                if (cancelToken.IsCancellationRequested)
                    return null;
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
            if (cancelToken.IsCancellationRequested)
                return null;
            return result;
        }

        /// <summary>
        /// 招待されているグループ
        /// </summary>
        public Task<Groups> GetInvited()
        {
            return GetInvited(CancellationToken.None);
        }
        public async Task<Groups> GetInvited(CancellationToken cancelToken)
        {
            return await GET<Groups>(1, "groups/invited", cancelToken);
        }

        public Task<List<Group>> GetPublicGroupAll()
        {
            return GetPublicGroupAll(CancellationToken.None);
        }
        public async Task<List<Group>> GetPublicGroupAll(CancellationToken cancelToken)
        {
            List<Group> result = new List<Group>();
            int page = 1;
            while (true)
            {
                var res = await GET<Groups[]>(1, "public_groups", cancelToken, new Dictionary<string, string>
                {
                    { "with_archived", "true" },
                    { "count", "1000" },
                    { "page", page++.ToString() }
                });
                if (cancelToken.IsCancellationRequested)
                    return null;
                if (res == null || res.Length == 0 || res[0] == null || res[0].items == null || res[0].items.Length == 0)
                    break;
                result.AddRange(res[0].items);
                if (res[0].items.Length < 1000)
                    break;
            }
            if (cancelToken.IsCancellationRequested)
                return null;
            return result.ToList();
        }
        public Task<List<Group>> GetPublicGroupAll(string user_id)
        {
            return GetPublicGroupAll(user_id, CancellationToken.None);
        }
        public async Task<List<Group>> GetPublicGroupAll(string user_id, CancellationToken cancelToken)
        {
            List<Group> result = new List<Group>();
            Dictionary<string, string> data = new Dictionary<string, string> { { "with_archived", "true" } };
            while (true)
            {
                var res = await GET<VisibleGroups>(1, $"user/{user_id}/visible_groups", cancelToken, data);
                if (cancelToken.IsCancellationRequested)
                    return null;
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
            if (cancelToken.IsCancellationRequested)
                return null;
            return result;
        }
        public Task<List<Group>> GetPublicGroup(int page, int count)
        {
            return GetPublicGroup(page, count, CancellationToken.None);
        }
        public async Task<List<Group>> GetPublicGroup(int page, int count, CancellationToken cancelToken)
        {
            var res = await GET<Groups[]>(1, "public_groups", cancelToken, new Dictionary<string, string>
            {
                { "with_archived", "true" },
                { "count", count.ToString() },
                { "page", page.ToString() }
            });
            if (res == null || res.Length == 0 || res[0] == null || res[0].items == null)
                return new List<Group>();
            return res[0].items.ToList();
        }

        public Task<List<Group>> GetPrivateGroupAll()
        {
            return GetPrivateGroupAll(CancellationToken.None);
        }
        public async Task<List<Group>> GetPrivateGroupAll(CancellationToken cancelToken)
        {
            List<Group> result = new List<Group>();
            int page = 1;
            while (true)
            {
                var res = await GET<Groups[]>(3, "groups", cancelToken, new Dictionary<string, string>
                {
                    { "with_archived", "true" },
                    { "count", "1000" },
                    { "page", page++.ToString() }
                });
                if (cancelToken.IsCancellationRequested)
                    return null;
                if (res == null || res.Length == 0 || res[0] == null || res[0].items == null || res[0].items.Length == 0)
                    break;
                result.AddRange(res[0].items);
                if (res[0].items.Length < 1000)
                    break;
            }
            if (cancelToken.IsCancellationRequested)
                return null;
            return result;
        }
        public Task<List<Group>> GetPrivateGroup(int page, int count)
        {
            return GetPrivateGroup(page, count, CancellationToken.None);
        }
        public async Task<List<Group>> GetPrivateGroup(int page, int count, CancellationToken cancelToken)
        {
            var res = await GET<Groups[]>(3, "groups", cancelToken, new Dictionary<string, string>
            {
                { "with_archived", "true" },
                { "count", count.ToString() },
                { "page", page.ToString() }
            });
            if (res == null || res.Length == 0 || res[0] == null || res[0].items == null)
                return new List<Group>();
            return res[0].items.ToList();
        }

        public Task<Group> GetGroup(string group_id)
        {
            return GetGroup(group_id, CancellationToken.None);
        }
        public async Task<Group> GetGroup(string group_id, CancellationToken cancelToken)
        {
            return await GET<Group>(2, $"group/{group_id}", cancelToken, new Dictionary<string, string>
            {
                { "members_count", "1" },
                { "fields", "subleaders,group_bookmark_info" }
            });
        }
        public Task<UserSmall> GetGroupLeader(string group_id)
        {
            return GetGroupLeader(group_id, CancellationToken.None);
        }
        public async Task<UserSmall> GetGroupLeader(string group_id, CancellationToken cancelToken)
        {
            return (await GET<Members>(2, $"group/{group_id}/members", cancelToken, new Dictionary<string, string>
            {
                { "members_count", "1" },
                { "fields", "owner" }
            })).owner;
        }
        public Task<List<UserSmall>> GetGroupSubleaders(string group_id)
        {
            return GetGroupSubleaders(group_id, CancellationToken.None);
        }
        public async Task<List<UserSmall>> GetGroupSubleaders(string group_id, CancellationToken cancelToken)
        {
            return ((await GET<Members>(2, $"group/{group_id}/members", cancelToken, new Dictionary<string, string>
            {
                { "members_count", "1" },
                { "fields", "subleaders" }
            })).subleaders ?? new UserSmall[0]).ToList();
        }
        public Task<List<UserSmall>> GetGroupMembersAll(string group_id)
        {
            return GetGroupMembersAll(group_id, CancellationToken.None);
        }
        public async Task<List<UserSmall>> GetGroupMembersAll(string group_id, CancellationToken cancelToken)
        {
            var result = new List<UserSmall>();
            var data = new Dictionary<string, string> { { "members_count", "1000" } };
            while (true)
            {
                var res = await GET<Members>(1, $"group/{group_id}/members", cancelToken, data);
                if (cancelToken.IsCancellationRequested)
                    return null;
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
            if (cancelToken.IsCancellationRequested)
                return null;
            return result;
        }

        public Task<List<Chat>> GetThreads(string group_id, int count, string older_than = null, string newer_than = null)
        {
            return GetThreads(group_id, count, CancellationToken.None, older_than, newer_than);
        }
        public async Task<List<Chat>> GetThreads(string group_id, int count, CancellationToken cancelToken, string older_than = null, string newer_than = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string> { { "count", count.ToString() } };
            if (!string.IsNullOrEmpty(older_than))
                data.Add("older_than", older_than);
            if (!string.IsNullOrEmpty(newer_than))
                data.Add("newer_than", newer_than);
            return await GET<List<Chat>>(2, $"group/{group_id}/chats", cancelToken, data);
        }
        public Task<Reply> GetRepliesAll(string group_id, string chat_id)
        {
            return GetRepliesAll(group_id, chat_id, CancellationToken.None);
        }
        public async Task<Reply> GetRepliesAll(string group_id, string chat_id, CancellationToken cancelToken)
        {
            return await GET<Reply>(1, $"group/{group_id}/chats/replies", cancelToken, new Dictionary<string, string> { { "to", chat_id } });
        }
        public Task<List<PokeUserItem>> GetPokesAll(string group_id, string chat_id)
        {
            return GetPokesAll(group_id, chat_id, CancellationToken.None);
        }
        public async Task<List<PokeUserItem>> GetPokesAll(string group_id, string chat_id, CancellationToken cancelToken)
        {
            List<PokeUserItem> result = new List<PokeUserItem>();
            Dictionary<string, string> data = new Dictionary<string, string> { { "id", chat_id } };
            while (true)
            {
                var res = await GET<Pokes>(1, $"group/{group_id}/chats/pokes", cancelToken, data);
                if (cancelToken.IsCancellationRequested)
                    return null;
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
            if (cancelToken.IsCancellationRequested)
                return null;
            return result;
        }

        public Task<Notifications> GetNotifications(int count, string cursor = null)
        {
            return GetNotifications(count, CancellationToken.None, cursor);
        }
        public async Task<Notifications> GetNotifications(int count, CancellationToken cancelToken, string cursor = null)
        {
            Dictionary<string, string> data = new Dictionary<string, string> { { "count", count.ToString() } };
            if (!string.IsNullOrEmpty(cursor))
                data.Add("last_cursor", cursor);
            return await GET<Notifications>(2, "info/notifications", cancelToken, data);
        }

        public async Task<RequestResult> Like(string group_id, string chat_id)
        {
            return await POST<RequestResult>(1, $"group/{group_id}/chats/like", new Dictionary<string, string> { { "id", chat_id } });
        }
        public async Task<RequestResult> UnLike(string group_id, string chat_id)
        {
            return await POST<RequestResult>(1, $"group/{group_id}/chats/unlike", new Dictionary<string, string> { { "id", chat_id } });
        }
        public async Task<RequestResult> Boo(string group_id, string chat_id)
        {
            return await POST<RequestResult>(1, $"group/{group_id}/chats/boo", new Dictionary<string, string> { { "id", chat_id } });
        }
        public async Task<RequestResult> UnBoo(string group_id, string chat_id)
        {
            return await POST<RequestResult>(1, $"group/{group_id}/chats/unboo", new Dictionary<string, string> { { "id", chat_id } });
        }

        public async Task<RequestResult> Follow(string user_id)
        {
            return await POST<RequestResult>(1, "me/contacts", new Dictionary<string, string> { { "users", user_id } });
        }
        public async Task<RequestResult> UnFollow(string user_id)
        {
            return await POST<RequestResult>(1, "me/contacts/remove", new Dictionary<string, string> { { "users", user_id } });
        }

        public async Task<List<Asset>> Assets(List<string> files)
        {
            if (files == null || files.Count == 0)
                throw new ArgumentNullException("ファイルを1枚以上指定してください");
            foreach (string file in files)
                if (!File.Exists(file))
                    throw new FileNotFoundException();
            var result = new List<Asset>();
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
                        result.Add(JsonConvert.DeserializeObject<Asset>(content));
                    }
                }
            }
            return result;
        }
        public async Task<Chat> Chat(string group_id, string message, bool shout = false)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException("メッセージが指定されていません");
            return await POST<Chat>(1, $"group/{group_id}/chats", new Dictionary<string, string>
            {
                { "type", shout ? "shout" : "normal" },
                { "message", message }
            });
        }
        public async Task<Chat> Chat(string group_id, string message, List<string> images, bool shout = false)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException("メッセージが指定されていません");
            var assets = await Assets(images);
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "type", shout ? "shout" : "normal" },
                { "message", message }
            };
            if (assets.Count > 0)
                data.Add("assets", string.Join(",", assets.Select(d => d.id + ":image")));
            return await POST<Chat>(1, $"group/{group_id}/chats", data);
        }
        public async Task<Chat> Chat(string group_id, string message, string reply_to)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException("メッセージが指定されていません");
            if (string.IsNullOrEmpty(reply_to))
                throw new ArgumentNullException("返信先が指定されていません");
            return await POST<Chat>(1, $"group/{group_id}/chats", new Dictionary<string, string>
            {
                { "type", "normal" },
                { "reply_to", reply_to },
                { "message", message }
            });
        }
        public async Task<Chat> Chat(string group_id, string message, List<string> images, string reply_to)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException("メッセージが指定されていません");
            if (string.IsNullOrEmpty(reply_to))
                throw new ArgumentNullException("返信先が指定されていません");
            var assets = await Assets(images);
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "type", "normal" },
                { "reply_to", reply_to },
                { "message", message }
            };
            if (assets.Count > 0)
                data.Add("assets", string.Join(",", assets.Select(d => d.id + ":image")));
            return await POST<Chat>(1, $"group/{group_id}/chats", data);
        }
        public async Task<RequestResult> RemoveChat(string group_id, string chat_id)
        {
            return await POST<RequestResult>(1, $"group/{group_id}/chats/remove", new Dictionary<string, string> { { "id", chat_id } });
        }

        public async Task<Group> Join(string group_id)
        {
            return await POST<Group>(1, $"group/{group_id}/join", new Dictionary<string, string>());
        }
        /// <summary>
        /// 招待を拒否
        /// </summary>
        public async Task<RequestResult> RefuseInvitation(string group_id)
        {
            return await POST<RequestResult>(1, $"group/{group_id}/refuse_invitation", new Dictionary<string, string>());
        }
        public async Task<RequestResult> Kick(string group_id, string user_id)
        {
            return await POST<RequestResult>(1, $"group/{group_id}/kick", new Dictionary<string, string> { { "target_user", user_id } });
        }
        public async Task<Group> LeaderTransfer(string group_id, string user_id)
        {
            return await POST<Group>(1, $"group/{group_id}/transfer", new Dictionary<string, string> { { "target_user", user_id } });
        }
        public async Task<RequestResult> SetSubleader(string group_id, string user_id)
        {
            return await POST<RequestResult>(1, $"group/{group_id}/subleaders", new Dictionary<string, string> { { "user", user_id } });
        }
        public async Task<RequestResult> RemoveSubleader(string group_id, string user_id)
        {
            return await POST<RequestResult>(1, $"group/{group_id}/subleaders/remove", new Dictionary<string, string> { { "user", user_id } });
        }

        public async Task<RequestResult> Block(string user_id)
        {
            return await POST<RequestResult>(1, "me/blocking_users", new Dictionary<string, string> { { "users", user_id } });
        }
        public async Task<RequestResult> Unblock(string user_id)
        {
            return await POST<RequestResult>(1, "me/blocking_users/remove", new Dictionary<string, string> { { "users", user_id } });
        }

        private async Task<T> GET<T>(int version, string request_url, CancellationToken cancelToken, Dictionary<string, string> query = null)
        {
            using (HttpClientHandler handler = new HttpClientHandler())
            using (HttpClient client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                client.DefaultRequestHeaders.Add("Host", "api.lobi.co");
                handler.AutomaticDecompression = DecompressionMethods.GZip;
                string url = $"https://api.lobi.co/{version}/{request_url}?platform={platform}&lang=ja&token={Token}{string.Join("", (query ?? new Dictionary<string, string>()).Select(d => $"&{WebUtility.UrlEncode(d.Key)}={WebUtility.UrlEncode(d.Value)}"))}";
                try
                {
                    using (var res = await client.GetAsync(url, (CancellationToken)cancelToken))
                    {
                        if (res.StatusCode != HttpStatusCode.OK)
                            throw new RequestAPIException(new ErrorObject(res));
                        return JsonConvert.DeserializeObject<T>(await res.Content.ReadAsStringAsync());
                    }
                }
                catch (OperationCanceledException)
                {
                    return default(T);
                }
            }
        }
        private Task<T> GET<T>(int version, string request_url, Dictionary<string, string> query = null)
        {
            return GET<T>(version, request_url, CancellationToken.None, query);
        }
        private async Task<T> POST<T>(int version, string request_url, Dictionary<string, string> query, CancellationToken cancelToken)
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
                string url = $"https://api.lobi.co/{version}/{request_url}";
                FormUrlEncodedContent post_data = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "lang", "ja" },
                    { "token", Token }
                }.Concat(query));
                try
                {
                    using (var res = await client.PostAsync(url, post_data, cancelToken))
                    {
                        if (res.StatusCode != HttpStatusCode.OK)
                            throw new RequestAPIException(new ErrorObject(res));
                        string result = await res.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<T>(result);
                    }
                }
                catch (OperationCanceledException)
                {
                    return default(T);
                }
            }
        }
        private Task<T> POST<T>(int version, string request_url, Dictionary<string, string> query)
        {
            return POST<T>(version, request_url, query, CancellationToken.None);
        }
    }
}
