using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VRSRBot.Util;
using VRSRBot.Entities;
using Octokit;

namespace VRSRBot.Core
{
    class Program
    {
        public static Config Config;
        public static Bot Bot;
        public static List<Game> Games;
        public static dynamic GameColors;

        static void Main(string[] args)
        {
            MiscMethods.Log("Starting...");

            if (!Directory.Exists("files"))
            {
                Directory.CreateDirectory("files");
                MiscMethods.Log("'files' directory not found. Directory created.", "&e");
            }
            if (!File.Exists("files/config.json"))
            {
                File.WriteAllText("files/config.json", JsonConvert.SerializeObject(new Config(), Formatting.Indented));
                MiscMethods.Log("'files/config.json' file not found. File created.", "&e");
            }

            Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("files/config.json"));
            if (Config.Token == "")
            {
                MiscMethods.Log("Error: You need to fill out the 'token' field in 'files/config.json' before starting.\n\n&7Press any key to continue...", "&c");
                Console.Read();
                return;
            }

            if (!File.Exists("files/userpass.txt"))
            {
                File.WriteAllText("files/userpass.txt", "");
            }

            Credentials creds;
            var credsFile = File.ReadAllLines("files/userpass.txt");
            if (credsFile.Length > 1)
            {
                creds = new Credentials(credsFile[0], credsFile[1]);
            }
            else
            {
                Console.WriteLine("Please fill out `userpass.txt` with the following:\nGitHub username on line 1\nGitHub password/access token on line 2");
                Console.ReadLine();
                return;
            }

            if (!System.Diagnostics.Debugger.IsAttached)
            {
                Heartbeat.Start(creds);
            }

            MiscMethods.Log("Loaded config.");

            using (WebClient wc = new WebClient())
            {
                var temp = wc.DownloadString("https://vrspeed.run/vrsrassets/other/games.json");
                Games = JsonConvert.DeserializeObject<List<Game>>(temp);

                temp = wc.DownloadString("https://vrspeed.run/vrsrassets/other/colors.json");
                GameColors = JsonConvert.DeserializeObject(temp);
            }

            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            MiscMethods.Log("Starting MainAsync...", "&2");

            Bot = new Bot(Config);

            await Task.Delay(-1);
        }
    }
}
