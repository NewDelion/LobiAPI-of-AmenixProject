using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupStreamingAPISample
{
    class Program
    {
        static void Main(string[] args)
        {
            var api = new LobiAPI.BasicAPI();
            Login(api);
            string group_id = SelectGroup(api);
            var stream = new LobiAPI.GroupStreamingAPI(api.Token);
            stream.StreamOpen(group_id);
            stream.AddHandler(group_id, Chat);
            stream.AddHandler(group_id, ChatDeleted);
            stream.AddHandler(group_id, Part);
            stream.AddHandler(group_id, (LobiAPI.StreamArchiveEventHandler)Archive);
            stream.AddHandler(group_id, (LobiAPI.StreamConnectedEvent)Connected);
            stream.AddHandler(group_id, (LobiAPI.StreamDisconnectedEvent)Disconnected);
            stream.AddHandler(group_id, (LobiAPI.StreamFailConnectEvent)FailConnect);
            stream.StreamConnect(group_id);
            Console.Clear();
            Console.WriteLine("エンターで終了します");
            Console.ReadLine();
        }

        static void Login(LobiAPI.BasicAPI api)
        {
            while (true)
            {
                Console.Clear();
                Console.Write("Mail: ");
                string mail = Console.ReadLine();
                Console.Write("Password: ");
                string password = Console.ReadLine();

                if (api.Login(mail, password).Result)
                    break;
                Console.WriteLine("ログイン失敗....");
            }
            Console.WriteLine("ログインしました。");
            Console.WriteLine();
        }

        static string SelectGroup(LobiAPI.BasicAPI api)
        {
            var groups = api.GetPublicGroupAll().Result.Concat(api.GetPrivateGroupAll().Result).ToList();
            while (true)
            {
                Console.Clear();
                Console.WriteLine("グループを選択してください");
                string format = @"[{0," + groups.Count.ToString().Length + @"}] {1}";
                foreach (var g in groups.Select((group, index) => new { group, index }))
                    Console.WriteLine(format, g.index + 1, g.group.name);
                Console.Write("番号を入力：");
                int input = -1;
                if(!int.TryParse(Console.ReadLine(), out input))
                {
                    Console.WriteLine("番号を入力してください");
                    continue;
                }
                if(input < 1 || input > groups.Count)
                {
                    Console.WriteLine("範囲外の番号が入力されました");
                    continue;
                }
                return groups[input - 1].uid;
            }
        }

        static void Connected(string group_id)
        {
            Console.WriteLine("# Connected!!");
        }

        static void Disconnected(string group_id)
        {
            Console.WriteLine("# Disconnected...");
        }

        static void FailConnect(string group_id, Exception ex)
        {
            Console.WriteLine("# FailConnect...");
        }

        static void Chat(string group_id, LobiAPI.Json.Chat chat)
        {
            Console.WriteLine(chat.message);
        }

        static void ChatDeleted(string group_id, string chat_id)
        {
            Console.WriteLine("チャットが削除されました({0})", chat_id);
        }

        static void Part(string group_id, LobiAPI.Json.User user)
        {
            Console.WriteLine("{0}さんがグループから抜けました", user.name);
        }

        static void Archive(string group_id)
        {
            Console.WriteLine("グループがアーカイブ状態になりました");
        }
    }
}
