using Canopy.Api.Client;
using System;
using System.Text;

namespace Canopy.Cli.Executable
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // TODO: Use command line argument parser to split into proper commands:
                // .NET Core Article: https://msdn.microsoft.com/en-us/magazine/mt763239.aspx
                // Example usage: https://github.com/ronnieoverby/AtomicUtils/tree/master/src/AtomicUtils

                if (string.IsNullOrWhiteSpace(CanopyAuthentication.ClientId) ||
                    string.IsNullOrWhiteSpace(CanopyAuthentication.ClientSecret))
                {
                    Console.WriteLine("Please set up ClientID and ClientSecret in CanopyAuthentication.");
                }

                if (!CanopyAuthentication.Instance.LoadAuthenticatedUser())
                {
                    Console.Write("Username: ");
                    var username = Console.ReadLine();
                    Console.Write("Company: ");
                    var company = Console.ReadLine();
                    Console.Write("Password: ");
                    var password = GetPassword();

                    Console.WriteLine();
                    Console.WriteLine();

                    CanopyAuthentication.Instance.SetAuthenticationInformation(username, company, password);
                }

                while (true)
                {
                    Console.WriteLine("Requesting cars...");
                    var authenticatedUser = CanopyAuthentication.Instance.GetAuthenticatedUser().Result;
                    var configClient = new ConfigClient(CanopyAuthentication.Configuration);
                    var result = configClient.GetConfigsAsyncAsync(authenticatedUser.TenantId, "car", null, null).Result;
                    foreach (var item in result.QueryResults.Documents)
                    {
                        Console.WriteLine(item.Name);
                    }

                    System.Threading.Thread.Sleep(TimeSpan.FromMinutes(5));
                }
            }
            catch (Exception t)
            {
                Console.WriteLine();
                Console.WriteLine(t);
            }
        }

        public static string GetPassword()
        {
            var pwd = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (i.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0)
                    {
                        pwd.Remove(pwd.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                }
                else
                {
                    pwd.Append(i.KeyChar);
                    Console.Write("*");
                }
            }
            return pwd.ToString();
        }
    }
}