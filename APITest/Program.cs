using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LobiAPI;
using LobiAPI.Json;

namespace APITest
{
    class Program
    {
        static void Main(string[] args)
        {
            var api = new BasicAPI();
            Login(api);
            var me = api.GetMe().Result;
            var me_contacts = api.GetContacts().Result;
            var me_followers = api.GetFollowers().Result;
            var user = api.GetUser("402aeea6d30bfbce06f79b61f5776991e5c82e02").Result;
            var user_contacts = api.GetContacts("402aeea6d30bfbce06f79b61f5776991e5c82e02").Result;
            var user_followers = api.GetFollowers("402aeea6d30bfbce06f79b61f5776991e5c82e02").Result;
            
            Console.ReadLine();
        }

        static void Login(BasicAPI api)
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
    }
}
