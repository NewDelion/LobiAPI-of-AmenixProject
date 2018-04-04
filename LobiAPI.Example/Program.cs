using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobiAPI.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var api = new BasicAPI();
            Login(api);
            var me = api.GetMe();

            Console.ReadKey();
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

                if (api.Login(mail, password, LoginServiceType.Lobi))
                    break;
                Console.WriteLine("ログイン失敗....");
                Console.ReadKey();
            }
            Console.WriteLine("ログインしました。");
            Console.ReadKey();
        }
    }
}
