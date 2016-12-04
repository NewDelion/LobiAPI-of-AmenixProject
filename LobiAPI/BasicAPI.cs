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
