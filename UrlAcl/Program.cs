using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security;
using System.Security.Principal;
using UrlAclLib;

namespace UrlAcl
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("?invalid format: urlacl { get prefix } | { add prefix { user } }");
                return; 
            }

            switch (args[0])
            {
                case "get":
                    get(args);
                    break;

                case "add":
                    add(args);
                    break;

                case "del":
                    del(args);
                    break;

                default:
                    break;
            }
        }

        static void get(string[] args)
        {
            using (var api = new HttpApi())
            {
                if (args.Length == 2)
                {
                    var acl = api.GetAcl(args[1]);
                    if (acl == null)
                        Console.WriteLine("None");
                    else
                        Console.WriteLine("Prefix: " + acl.Prefix + " Acl: " + acl.Acl);
                }
                else
                {
                    var list = api.GetAcl();
                    foreach (var a in list)
                    {
                        Console.WriteLine("Prefix: " + a.Prefix + " Acl: " + a.Acl);
                    }
                }
            }
        }

        static void add(string[] args)
        {
            string user = null;
            if (args.Length == 2)
            {
                user = Environment.UserName;
            }
            else if (args.Length == 3)
            {
                user = args[2];
            }

            if (user == null)
            {
                Console.WriteLine("?invalid add, format: urlacl add {prefix} {user}");
                return;
            }

            using (var api = new HttpApi())
            {
                var sid = new WindowsIdentity(user).User;
                var dacl = "D:(A;;GX;;;" + sid + ")";
                var isSet = api.SetAcl(args[1], dacl);
                if (isSet)
                    Console.WriteLine("Success");
                else
                    Console.WriteLine("Failed to set prefix: " + args[1]);
            }

        }


        static void del(string[] args)
        {
            string user = null;
            if (args.Length == 2)
            {
                user = Environment.UserName;
            }
            else if (args.Length == 3)
            {
                user = args[2];
            }

            if (user == null)
            {
                Console.WriteLine("?invalid add, format: urlacl add {prefix} {user}");
                return;
            }

            using (var api = new HttpApi())
            {
                var sid = new WindowsIdentity(user).User;
                var dacl = "D:(A;;GX;;;" + sid + ")";
                var isSet = api.DelAcl(args[1], dacl);
                if (isSet)
                    Console.WriteLine("Success");
                else
                    Console.WriteLine("Failed to set prefix: " + args[1]);
            }

        }
    }
}
