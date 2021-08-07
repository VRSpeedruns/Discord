using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VRSRBot.Util;

namespace VRSRBot.Core
{
    class Program
    {
        public static Config Config;
        public static Bot Bot;
        public static List<Game> Games;

        static void Main(string[] args)
        {
            Log("Starting...");

            if (!Directory.Exists("files"))
            {
                Directory.CreateDirectory("files");
                Log("'files' directory not found. Directory created.", "&e");
            }
            if (!File.Exists("files/config.json"))
            {
                File.WriteAllText("files/config.json", JsonConvert.SerializeObject(new Config(), Formatting.Indented));
                Log("'files/config.json' file not found. File created.", "&e");
            }

            Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("files/config.json"));
            if (Config.Token == "")
            {
                Log("Error: You need to fill out the 'token' field in 'files/config.json' before starting.\n\n&7Press any key to continue...", "&c");
                Console.Read();
                return;
            }

            Log("Loaded config.");

            using (WebClient wc = new WebClient())
            {
                var temp = wc.DownloadString("https://vrspeed.run/vrsrassets/other/games.json");
                Games = JsonConvert.DeserializeObject<List<Game>>(temp);
            }

            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            Log("Starting MainAsync...", "&2");

            Bot = new Bot(Config);

            await Task.Delay(-1);
        }

        public static void Log(string message, string color = "&7")
        {
            FConsole.WriteLine($"{color}[{DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt")}]%0&f {message}");
        }
    }
}
