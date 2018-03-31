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

        private string GetCsrf()
        {
            _Client.UserAgent = "Mozilla/5.0 (Linux; Android 5.1; Google Nexus 10 - 5.1.0 - API 22 - 2560x1600 Build/LMY47D) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/39.0.0.0 Safari/537.36 Lobi/8.10.3";
            _Client.CookieContainer = new CookieContainer();

            var req_get_csrf = new RestRequest("https://lobi.co/inapp/signin/password", Method.GET);
            req_get_csrf.AddParameter("webview", "1", ParameterType.UrlSegment);
            return _Client.Execute(req_get_csrf).Content.SelectSingleHtmlNodeAttribute("//input[@name='csrf_token']", "value");
        }
        private string GetSpell(string mail, string password)
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
        private string GetToken(string device_uuid, string spell)
        {
            string sig;
            using (var mac = new HMACSHA1(Encoding.ASCII.GetBytes("db6db1788023ce4703eecf6aa33f5fcde35a458c")))
            using (var stream = new MemoryStream(Encoding.ASCII.GetBytes(spell)))
                sig = Convert.ToBase64String(mac.ComputeHash(stream));

            var req = new RestRequest("1/signin_confirmation", Method.POST);
            req.AddParameter("device_uuid", DeviceUUID, ParameterType.GetOrPost);
            req.AddParameter("sig", sig, ParameterType.GetOrPost);
            req.AddParameter("spell", spell, ParameterType.GetOrPost);
            return _Client.Execute<UserMinimalWithToken>(req).Data.Token;
        }
        public bool Login(string mail, string password)
        {
            string spell = GetSpell(mail, password);
            if (spell == null || spell == "")
                return false;
            DeviceUUID = Guid.NewGuid().ToString();
            Token = GetToken(DeviceUUID, spell);
            return Token != null && (Token ?? "").Length > 0;
        }

        private long NowUnixtime => (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        private string RandomNonce => BitConverter.ToString(BitConverter.GetBytes(DateTime.Now.Ticks)).Replace("-", "");
        public bool TwitterLogin(string mail, string password)
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
    }
}
