using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace ELearningCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            Options options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                Start(options);
            }
            else
            {
                CommandLine.Text.HelpText txt = CommandLine.Text.HelpText.AutoBuild(options);
                Console.WriteLine(txt.ToString());
            }

            Console.ReadLine();
        }

        static async void Start(Options options)
        {
            Crawler c = new Crawler();

            await c.LoginToELeraning(options.Username, options.Password);
            c.DownloadAllInFolder(null);
        }
    }

    class Options
    {
        [Option('u', "user", Required = true, HelpText = "Anmeldename, in der Regel die K-Nummer")]
        public string Username { get; set; }
        [Option('p', "password", Required = true, HelpText = "Dein Passwort")]
        public string Password { get; set; }
    }
}
