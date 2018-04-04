using HtmlAgilityPack;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using LobiAPI.Json;
using System.Threading;

namespace LobiAPI
{
    public class BasicAPI
    {
        protected RestClient _Client = null;
        protected HtmlDocument _Doc = null;

        protected readonly string UserAgent = "Nakamap 14.4.1 Android 7.1.1 unknown hidden hidden";
        protected readonly string platform = "android";
        protected string DeviceUUID = "";
        public string Token
        {
            get
            {
                //DefaultParametersの先頭にAcceptが入ってるから気を付けて！！
                if (_Client.DefaultParameters.Count < 4)
                    return null;
                return (string)_Client.DefaultParameters[3].Value;
            }
            set
            {
                //DefaultParametersの先頭にAcceptが入ってるから気を付けて！！
                if (_Client.DefaultParameters.Count < 4)
                    _Client.AddDefaultParameter("token", value);
                else
                    _Client.DefaultParameters[3].Value = value;
            }
        }

        public BasicAPI()
        {
            _Client = new RestClient("https://api.lobi.co");
            _Client.UserAgent = UserAgent;
            _Client.AddDefaultParameter("lang", "ja");
            _Client.AddDefaultParameter("platform", platform);
            _Client.FollowRedirects = false;

            _Doc = new HtmlDocument();
        }

        #region Login関連

        public bool Login(string mail, string password, LoginServiceType service)
        {
            switch (service)
            {
                case LoginServiceType.Lobi:
                    return LoginByLobi(mail, password);
                case LoginServiceType.Twitter:
                    return LoginByTwitter(mail, password);
                default:
                    throw new NotSupportedException("指定されたサービスでのログイン処理はサポートしていません");
            }
        }

        public Task<bool> LoginAsync(string mail, string password, LoginServiceType service)
        {
            switch (service)
            {
                case LoginServiceType.Lobi:
                    return LoginByLobiAsync(mail, password);
                case LoginServiceType.Twitter:
                    return LoginByTwitterAsync(mail, password);
                default:
                    throw new NotSupportedException("指定されたサービスでのログイン処理はサポートしていません");
            }
        }

        public bool LoginByLobi(string mail, string password)
        {
            string GetCsrf()
            {
                _Client.UserAgent = "Mozilla/5.0 (Linux; Android 5.1; Google Nexus 10 - 5.1.0 - API 22 - 2560x1600 Build/LMY47D) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/39.0.0.0 Safari/537.36 Lobi/8.10.3";
                _Client.CookieContainer = new CookieContainer();

                var req_get_csrf = new RestRequest("https://lobi.co/inapp/signin/password", Method.GET);
                req_get_csrf.AddParameter("webview", "1", ParameterType.UrlSegment);
                return _Client.Execute(req_get_csrf).Content.SelectSingleHtmlNodeAttribute("//input[@name='csrf_token']", "value");
            }

            string GetSpell()
            {
                var req_get_spell = new RestRequest("https://lobi.co/inapp/signin/password", Method.POST);
                req_get_spell.AddParameter("csrf_token", GetCsrf(), ParameterType.GetOrPost);
                req_get_spell.AddParameter("email", mail, ParameterType.GetOrPost);
                req_get_spell.AddParameter("password", password, ParameterType.GetOrPost);
                var location_header = _Client.Execute(req_get_spell).Headers.FirstOrDefault(d => d.Name == "Location");
                _Client.UserAgent = UserAgent;
                _Client.CookieContainer = null;
                string location = location_header == null ? null : (string)location_header.Value;
                string prefix = "nakamapbridge://signin?spell=";
                if (location == null || !location.StartsWith(prefix))
                    return "";
                return location.Substring(prefix.Length);
            }

            string Spell = GetSpell();
            if (string.IsNullOrEmpty(Spell))
                return false;
            DeviceUUID = Guid.NewGuid().ToString();

            string GetToken()
            {
                string sig;
                using (var mac = new HMACSHA1(Encoding.ASCII.GetBytes("db6db1788023ce4703eecf6aa33f5fcde35a458c")))
                using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(Spell)))
                    sig = Convert.ToBase64String(mac.ComputeHash(stream));

                var req = new RestRequest("1/signin_confirmation", Method.POST);
                req.AddParameter("device_uuid", DeviceUUID, ParameterType.GetOrPost);
                req.AddParameter("sig", sig, ParameterType.GetOrPost);
                req.AddParameter("spell", Spell, ParameterType.GetOrPost);
                return _Client.Execute<UserMinimalWithToken>(req).Data.Token;
            }

            Token = GetToken();
            return (Token ?? "").Length > 0;
        }

        public Task<bool> LoginByLobiAsync(string mail, string password) => LoginByLobiAsync(mail, password, CancellationToken.None);

        public async Task<bool> LoginByLobiAsync(string mail, string password, CancellationToken cancellationToken)
        {
            async Task<string> GetCsrfAsync()
            {
                _Client.UserAgent = "Mozilla/5.0 (Linux; Android 5.1; Google Nexus 10 - 5.1.0 - API 22 - 2560x1600 Build/LMY47D) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/39.0.0.0 Safari/537.36 Lobi/8.10.3";
                _Client.CookieContainer = new CookieContainer();

                var req_get_csrf = new RestRequest("https://lobi.co/inapp/signin/password", Method.GET);
                req_get_csrf.AddParameter("webview", "1", ParameterType.UrlSegment);
                return (await _Client.ExecuteTaskAsync(req_get_csrf, cancellationToken)).Content.SelectSingleHtmlNodeAttribute("//input[@name='csrf_token']", "value");
            }

            async Task<string> GetSpellAsync()
            {
                var req_get_spell = new RestRequest("https://lobi.co/inapp/signin/password", Method.POST);
                req_get_spell.AddParameter("csrf_token", await GetCsrfAsync(), ParameterType.GetOrPost);
                req_get_spell.AddParameter("email", mail, ParameterType.GetOrPost);
                req_get_spell.AddParameter("password", password, ParameterType.GetOrPost);
                var location_header = (await _Client.ExecuteTaskAsync(req_get_spell, cancellationToken)).Headers.FirstOrDefault(d => d.Name == "Location");
                _Client.UserAgent = UserAgent;
                _Client.CookieContainer = null;
                string location = location_header == null ? null : (string)location_header.Value;
                string prefix = "nakamapbridge://signin?spell=";
                if (location == null || !location.StartsWith(prefix))
                    return "";
                return location.Substring(prefix.Length);
            }

            string Spell = await GetSpellAsync();
            if (string.IsNullOrEmpty(Spell))
                return false;
            DeviceUUID = Guid.NewGuid().ToString();

            async Task<string> GetTokenAsync()
            {
                string sig;
                using (var mac = new HMACSHA1(Encoding.ASCII.GetBytes("db6db1788023ce4703eecf6aa33f5fcde35a458c")))
                using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(Spell)))
                    sig = Convert.ToBase64String(mac.ComputeHash(stream));
                
                var req = new RestRequest("1/signin_confirmation", Method.POST);
                req.AddParameter("device_uuid", DeviceUUID, ParameterType.GetOrPost);
                req.AddParameter("sig", sig, ParameterType.GetOrPost);
                req.AddParameter("spell", Spell, ParameterType.GetOrPost);
                return (await _Client.ExecuteTaskAsync<UserMinimalWithToken>(req, cancellationToken)).Data.Token;
            }

            Token = await GetTokenAsync();
            return (Token ?? "").Length > 0;
        }

        /// <summary>
        /// 何度もログイン処理を行うとTwitterアカウントがロックされる可能性があります。Tokenを再利用してこのメソッドの呼び出し頻度を減らしてください。
        /// </summary>
        public bool LoginByTwitter(string mail, string password)
        {
            const string TWITTER_CONSUMER_KEY = "ZrgdukWwDeXVg9NCWG7rA";
            const string TWITTER_CONSUMER_KEY_SECRET = "WYNDf3OcvrWD31tnKnGbQHXqmZq1fohwBuvMnIJSs";

            var client = new RestClient(new Uri("https://api.twitter.com/"))
            {
                Authenticator = OAuth1Authenticator.ForRequestToken(TWITTER_CONSUMER_KEY, TWITTER_CONSUMER_KEY_SECRET),
                CookieContainer = new CookieContainer()
            };

            var req = new RestRequest("oauth/request_token", Method.POST);
            var qs = HttpUtility.ParseQueryString(client.Execute(req).Content);
            var oauthToken = qs["oauth_token"];
            var oauthTokenSecret = qs["oauth_token_secret"];

            var req2 = new RestRequest("oauth/authorize", Method.GET);
            req2.AddParameter("oauth_token", oauthToken);
            var authenticity_token = client.Execute(req2).Content.SelectSingleHtmlNodeAttribute("//input[@name='authenticity_token']", "value");

            var req3 = new RestRequest("oauth/authorize", Method.POST);
            req3.AddParameter("oauth_token", oauthToken);
            req3.AddParameter("authenticity_token", authenticity_token);
            req3.AddParameter("session[username_or_email]", mail);
            req3.AddParameter("session[password]", password);
            var res = client.Execute(req3);
            var tmp = res.Content.SelectSingleHtmlNodeAttribute("//meta[@http-equiv='refresh']", "content");
            var verifier = tmp.Substring(tmp.LastIndexOf('=') + 1);

            var req4 = new RestRequest("oauth/access_token", Method.POST);
            client.Authenticator = OAuth1Authenticator.ForAccessToken(TWITTER_CONSUMER_KEY, TWITTER_CONSUMER_KEY_SECRET, oauthToken, oauthTokenSecret, verifier);
            var qs2 = HttpUtility.ParseQueryString(client.Execute(req4).Content);
            var accessToken = qs2["oauth_token"];
            var accessTokenSecret = qs2["oauth_token_secret"];

            DeviceUUID = Guid.NewGuid().ToString();

            var req5 = new RestRequest("1/signup", Method.POST);
            req5.AddParameter("service", "twitter");
            req5.AddParameter("device_uuid", DeviceUUID);
            req5.AddParameter("access_token", accessToken);
            req5.AddParameter("access_token_secret", accessTokenSecret);
            Token = _Client.Execute<UserMinimalWithToken>(req5).Data.Token;
            return true;
        }

        /// <summary>
        /// 何度もログイン処理を行うとTwitterアカウントがロックされる可能性があります。Tokenを再利用してこのメソッドの呼び出し頻度を減らしてください。
        /// </summary>
        public Task<bool> LoginByTwitterAsync(string mail, string password) => LoginByTwitterAsync(mail, password, CancellationToken.None);

        /// <summary>
        /// 何度もログイン処理を行うとTwitterアカウントがロックされる可能性があります。Tokenを再利用してこのメソッドの呼び出し頻度を減らしてください。
        /// </summary>
        public async Task<bool> LoginByTwitterAsync(string mail, string password, CancellationToken cancellationToken)
        {
            const string TWITTER_CONSUMER_KEY = "ZrgdukWwDeXVg9NCWG7rA";
            const string TWITTER_CONSUMER_KEY_SECRET = "WYNDf3OcvrWD31tnKnGbQHXqmZq1fohwBuvMnIJSs";

            var client = new RestClient(new Uri("https://api.twitter.com/"))
            {
                Authenticator = OAuth1Authenticator.ForRequestToken(TWITTER_CONSUMER_KEY, TWITTER_CONSUMER_KEY_SECRET),
                CookieContainer = new CookieContainer()
            };

            var req = new RestRequest("oauth/request_token", Method.POST);
            var qs = HttpUtility.ParseQueryString((await client.ExecuteTaskAsync(req, cancellationToken)).Content);
            var oauthToken = qs["oauth_token"];
            var oauthTokenSecret = qs["oauth_token_secret"];

            var req2 = new RestRequest("oauth/authorize", Method.GET);
            req2.AddParameter("oauth_token", oauthToken);
            var authenticity_token = (await client.ExecuteTaskAsync(req2, cancellationToken)).Content.SelectSingleHtmlNodeAttribute("//input[@name='authenticity_token']", "value");

            var req3 = new RestRequest("oauth/authorize", Method.POST);
            req3.AddParameter("oauth_token", oauthToken);
            req3.AddParameter("authenticity_token", authenticity_token);
            req3.AddParameter("session[username_or_email]", mail);
            req3.AddParameter("session[password]", password);
            var res = await client.ExecuteTaskAsync(req3, cancellationToken);
            var tmp = res.Content.SelectSingleHtmlNodeAttribute("//meta[@http-equiv='refresh']", "content");
            var verifier = tmp.Substring(tmp.LastIndexOf('=') + 1);

            var req4 = new RestRequest("oauth/access_token", Method.POST);
            client.Authenticator = OAuth1Authenticator.ForAccessToken(TWITTER_CONSUMER_KEY, TWITTER_CONSUMER_KEY_SECRET, oauthToken, oauthTokenSecret, verifier);
            var qs2 = HttpUtility.ParseQueryString((await client.ExecuteTaskAsync(req4, cancellationToken)).Content);
            var accessToken = qs2["oauth_token"];
            var accessTokenSecret = qs2["oauth_token_secret"];

            DeviceUUID = Guid.NewGuid().ToString();

            var req5 = new RestRequest("1/signup", Method.POST);
            req5.AddParameter("service", "twitter");
            req5.AddParameter("device_uuid", DeviceUUID);
            req5.AddParameter("access_token", accessToken);
            req5.AddParameter("access_token_secret", accessTokenSecret);
            Token = (await _Client.ExecuteTaskAsync<UserMinimalWithToken>(req5, cancellationToken)).Data.Token;
            return true;
        }

        #endregion

        #region GetMe()

        public UserInfo GetMe() => Get<UserInfo>("1/me", new Dictionary<string, string> { { "fields", "premium,public_groups_count" } });
        public Task<UserInfo> GetMeAsync() => GetMeAsync(CancellationToken.None);
        public Task<UserInfo> GetMeAsync(CancellationToken cancellationToken) => GetAsync<UserInfo>("1/me", new Dictionary<string, string> { { "fields", "premium,public_groups_count" } }, cancellationToken);
        
        #endregion

        #region Contact関連

        public Users GetContacts(string cursor) => Get<Users>("3/me/contacts", new Dictionary<string, string> { { "cursor", cursor } });
        public Task<Users> GetContactsAsync(string cursor) => GetContactsAsync(cursor, CancellationToken.None);
        public Task<Users> GetContactsAsync(string cursor, CancellationToken cancellationToken) => GetAsync<Users>("3/me/contacts", new Dictionary<string, string> { { "cursor", cursor } }, cancellationToken);

        public Users GetContactsAll()
        {
            Users result = GetContacts("");
            while (result != null && result.NextCursor != null && result.NextCursor != "-1")
            {
                var tmp = GetContacts(result.NextCursor);
                if (tmp == null)
                    break;//throwの方がいいのかな？
                result.NextCursor = tmp.NextCursor;
                result.UserList.AddRange(tmp.UserList);
            }
            return result;
        }
        public Task<Users> GetContactsAllAsync() => GetContactsAllAsync(CancellationToken.None);
        public async Task<Users> GetContactsAllAsync(CancellationToken cancellationToken)
        {
            Users result = await GetContactsAsync("", cancellationToken);
            while(result != null && result.NextCursor != null && result.NextCursor != "-1")
            {
                var tmp = await GetContactsAsync(result.NextCursor, cancellationToken);
                if (tmp == null)
                    break;//throwの方がいいのかな？
                result.NextCursor = tmp.NextCursor;
                result.UserList.AddRange(tmp.UserList);
            }
            return result;
        }

        public Users GetContacts(string user_id, string cursor) => Get<Users>($"1/user/{user_id}/contacts", new Dictionary<string, string> { { "cursor", cursor } });
        public Task<Users> GetContactsAsync(string user_id, string cursor) => GetContactsAsync(user_id, cursor, CancellationToken.None);
        public Task<Users> GetContactsAsync(string user_id, string cursor, CancellationToken cancellationToken) => GetAsync<Users>($"1/user/{user_id}/contacts", new Dictionary<string, string> { { "cursor", cursor } }, cancellationToken);

        public Users GetContactsAll(string user_id)
        {
            Users result = GetContacts(user_id, "");
            while (result != null && result.NextCursor != null && result.NextCursor != "-1")
            {
                var tmp = GetContacts(user_id, result.NextCursor);
                if (tmp == null)
                    break;//throwの方がいいのかな？
                result.NextCursor = tmp.NextCursor;
                result.UserList.AddRange(tmp.UserList);
            }
            return result;
        }
        public Task<Users> GetContactsAllAsync(string user_id) => GetContactsAllAsync(user_id, CancellationToken.None);
        public async Task<Users> GetContactsAllAsync(string user_id, CancellationToken cancellationToken)
        {
            Users result = await GetContactsAsync(user_id, "", cancellationToken);
            while (result != null && result.NextCursor != null && result.NextCursor != "-1")
            {
                var tmp = await GetContactsAsync(user_id, result.NextCursor, cancellationToken);
                if (tmp == null)
                    break;//throwの方がいいのかな？
                result.NextCursor = tmp.NextCursor;
                result.UserList.AddRange(tmp.UserList);
            }
            return result;
        }

        #endregion

        #region Follower関連

        public Users GetFollowers(string cursor) => Get<Users>("2/me/followers", new Dictionary<string, string> { { "cursor", cursor } });
        public Task<Users> GetFollowersAsync(string cursor) => GetFollowersAsync(cursor, CancellationToken.None);
        public Task<Users> GetFollowersAsync(string cursor, CancellationToken cancellationToken) => GetAsync<Users>("2/me/followers", new Dictionary<string, string> { { "cursor", cursor } }, cancellationToken);

        public Users GetFollowersAll()
        {
            Users result = GetFollowers("");
            while(result != null && result.NextCursor != null && result.NextCursor != "-1")
            {
                var tmp = GetFollowers(result.NextCursor);
                if (tmp == null)
                    break;//throwの方がいいのかな？
                result.NextCursor = tmp.NextCursor;
                result.UserList.AddRange(tmp.UserList);
            }
            return result;
        }
        public Task<Users> GetFollowersAllAsync() => GetFollowersAllAsync(CancellationToken.None);
        public async Task<Users> GetFollowersAllAsync(CancellationToken cancellationToken)
        {
            Users result = await GetFollowersAsync("", cancellationToken);
            while (result != null && result.NextCursor != null && result.NextCursor != "-1")
            {
                var tmp = await GetFollowersAsync(result.NextCursor, cancellationToken);
                if (tmp == null)
                    break;//throwの方がいいのかな？
                result.NextCursor = tmp.NextCursor;
                result.UserList.AddRange(tmp.UserList);
            }
            return result;
        }

        public Users GetFollowers(string user_id, string cursor) => Get<Users>($"1/user/{user_id}/followers", new Dictionary<string, string> { { "cursor", cursor } });
        public Task<Users> GetFollowersAsync(string user_id, string cursor) => GetFollowersAsync(user_id, cursor, CancellationToken.None);
        public Task<Users> GetFollowersAsync(string user_id, string cursor, CancellationToken cancellationToken) => GetAsync<Users>($"1/user/{user_id}/followers", new Dictionary<string, string> { { "cursor", cursor } }, cancellationToken);

        public Users GetFollowersAll(string user_id)
        {
            Users result = GetFollowers(user_id, "");
            while (result != null && result.NextCursor != null && result.NextCursor != "-1")
            {
                var tmp = GetFollowers(user_id, result.NextCursor);
                if (tmp == null)
                    break;//throwの方がいいのかな？
                result.NextCursor = tmp.NextCursor;
                result.UserList.AddRange(tmp.UserList);
            }
            return result;
        }
        public Task<Users> GetFollowersAllAsync(string user_id) => GetFollowersAllAsync(user_id, CancellationToken.None);
        public async Task<Users> GetFollowersAllAsync(string user_id, CancellationToken cancellationToken)
        {
            Users result = await GetFollowersAsync(user_id, "", cancellationToken);
            while (result != null && result.NextCursor != null && result.NextCursor != "-1")
            {
                var tmp = await GetFollowersAsync(user_id, result.NextCursor, cancellationToken);
                if (tmp == null)
                    break;//throwの方がいいのかな？
                result.NextCursor = tmp.NextCursor;
                result.UserList.AddRange(tmp.UserList);
            }
            return result;
        }

        #endregion

        #region GetUser()

        public UserInfo GetUser(string user_id) => Get<UserInfo>($"1/user/{user_id}", new Dictionary<string, string> { { "fields", "is_blocked,public_groups_count" } });
        public Task<UserInfo> GetUserAsync(string user_id) => GetUserAsync(user_id, CancellationToken.None);
        public Task<UserInfo> GetUserAsync(string user_id, CancellationToken cancellationToken) => GetAsync<UserInfo>($"1/user/{user_id}", new Dictionary<string, string> { { "fields", "is_blocked,public_groups_count" } });

        #endregion

        #region GETメソッド

        protected T Get<T>(string endpoint) where T : new() => Get<T>(endpoint, Enumerable.Empty<KeyValuePair<string, string>>());
        protected T Get<T>(string endpoint, IEnumerable<KeyValuePair<string, string>> parameters) where T : new()
        {
            var req = new RestRequest(endpoint, Method.GET);
            foreach (var param in parameters)
                req.AddParameter(new Parameter { Name = param.Key, Value = param.Value, Type = ParameterType.GetOrPost });
            return _Client.Execute<T>(req).Data;
        }

        protected Task<T> GetAsync<T>(string endpoint) => GetAsync<T>(endpoint, Enumerable.Empty<KeyValuePair<string, string>>(), CancellationToken.None);
        protected Task<T> GetAsync<T>(string endpoint, IEnumerable<KeyValuePair<string, string>> parameters) => GetAsync<T>(endpoint, parameters, CancellationToken.None);
        protected Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken) => GetAsync<T>(endpoint, Enumerable.Empty<KeyValuePair<string, string>>(), cancellationToken);
        protected async Task<T> GetAsync<T>(string endpoint, IEnumerable<KeyValuePair<string, string>> parameters, CancellationToken cancellationToken)
        {
            var req = new RestRequest(endpoint, Method.GET);
            foreach (var param in parameters)
                req.AddParameter(new Parameter { Name = param.Key, Value = param.Value, Type = ParameterType.GetOrPost });
            return (await _Client.ExecuteTaskAsync<T>(req, cancellationToken)).Data;
        }

        #endregion
    }
}
