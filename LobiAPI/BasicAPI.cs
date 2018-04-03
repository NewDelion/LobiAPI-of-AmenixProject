﻿using HtmlAgilityPack;
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

        public Task<bool> Login(string mail, string password, LoginServiceType service)
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

        public Task<bool> LoginByLobi(string mail, string password) => LoginByLobi(mail, password, default(CancellationToken));

        public async Task<bool> LoginByLobi(string mail, string password, CancellationToken cancellationToken)
        {
            async Task<string> GetCsrf()
            {
                _Client.UserAgent = "Mozilla/5.0 (Linux; Android 5.1; Google Nexus 10 - 5.1.0 - API 22 - 2560x1600 Build/LMY47D) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/39.0.0.0 Safari/537.36 Lobi/8.10.3";
                _Client.CookieContainer = new CookieContainer();

                var req_get_csrf = new RestRequest("https://lobi.co/inapp/signin/password", Method.GET);
                req_get_csrf.AddParameter("webview", "1", ParameterType.UrlSegment);
                return (await _Client.ExecuteTaskAsync(req_get_csrf, cancellationToken)).Content.SelectSingleHtmlNodeAttribute("//input[@name='csrf_token']", "value");
            }

            async Task<string> GetSpell()
            {
                var req_get_spell = new RestRequest("https://lobi.co/inapp/signin/password", Method.POST);
                req_get_spell.AddParameter("csrf_token", await GetCsrf(), ParameterType.GetOrPost);
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

            string Spell = await GetSpell();
            if (string.IsNullOrEmpty(Spell))
                return false;
            DeviceUUID = Guid.NewGuid().ToString();

            async Task<string> GetToken()
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

            Token = await GetToken();
            return (Token ?? "").Length > 0;
        }

        /// <summary>
        /// 何度もログイン処理を行うとTwitterアカウントがロックされる可能性があります。Tokenを再利用してこのメソッドの呼び出し頻度を減らしてください。
        /// </summary>
        public Task<bool> LoginByTwitter(string mail, string password) => LoginByTwitter(mail, password, default(CancellationToken));

        /// <summary>
        /// 何度もログイン処理を行うとTwitterアカウントがロックされる可能性があります。Tokenを再利用してこのメソッドの呼び出し頻度を減らしてください。
        /// </summary>
        public async Task<bool> LoginByTwitter(string mail, string password, CancellationToken cancellationToken)
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
    }
}
