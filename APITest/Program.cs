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
