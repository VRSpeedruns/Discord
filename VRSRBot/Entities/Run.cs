using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using VRSRBot.Core;
using System.Text.RegularExpressions;
using System.Linq;

namespace VRSRBot.Entities
{
    class Run
    {
        public string ID;

        public string Response;

        public Run(string id)
        {
            ID = id;
        }

        public async Task DownloadData()
        {
            var url = $"https://www.speedrun.com/api/v1/runs/{ID}?embed=players,platform,game,category,category.variables";

            using (WebClient wc = new WebClient())
            {
                Response = await Bot.SRCAPICall(url, wc);
            }
        }

        public DiscordEmbed GetEmbed()
        {
            if (Response == "") return null;

            dynamic data = JsonConvert.DeserializeObject(Response);
            data = data.data;

            Game thisGame = null;
            foreach (var _game in Program.Games)
            {
                if (_game.id == (string)data.game.data.abbreviation)
                {
                    thisGame = _game;
                    break;
                }
            }
            if (thisGame == null) return null;

            string game = data.game.data.names.international;
            string category = data.category.data.name;
            string subcats = "";
            var _subcats = new List<string>();

            foreach (var variable in data.category.data.variables.data)
            {
                if ((bool)variable["is-subcategory"])
                {
                    _subcats.Add((string)variable.name);
                }
            }
            if (_subcats.Count > 0)
            {
                subcats = $"({string.Join(", ", _subcats)})";
            }

            var urlId = Regex.Replace(category.Replace(" ", "_"), @"[^a-zA-Z0-9-_]", "");

            var time = ((string)data.times.primary).Replace("PT", "").Replace("H", "h ").Replace("M", "m ");
            if (time.Contains("."))
            {
                time = time.Replace(".", "s ").Replace("S", "ms");
                var ms = time.Split("s ")[1].Split("ms")[0];
                ms = Regex.Replace(ms, @"^0", "");
                
                time = $"{time.Split("s ")[0]}s {ms}ms";
            }
            else
            {
                time = time.Replace("S", "s");
            }

            var player = "";
            var discord = "";

            if (data.players.data[0].rel == "user")
            {
                player = data.players.data[0].names.international;

                if (Bot.LinkedUsers.Any(u => u.SpeedruncomID == (string)data.players.data[0].id))
                {
                    discord = $" (<@{Bot.LinkedUsers.First(u => u.SpeedruncomID == (string)data.players.data[0].id).DiscordID}>)";
                }
            }
            else
            {
                player = data.players.data[0].name;
            }

            var comment = data.comment;
            if (comment != "")
                comment = $"*\"{comment}\"*\n\n";

            var embed = new DiscordEmbedBuilder()
            {
                Author = new DiscordEmbedBuilder.EmbedAuthor()
                {
                    Name = "🏆 NEW WORLD RECORD! 🏆"
                },
                Description = $"**[{game}](https://vrspeed.run/{thisGame.abbreviation})**" +
                    $" - **[{category}](https://vrspeed.run/{thisGame.abbreviation}#{urlId})** {subcats}\n\n" +
                    $"Run completed in **{time}** by **{player}**{discord}\n\n" +
                    $"{comment}" +
                    $"<:vrsr:873137783630360636> **[View run on VRSpeed.run]({data.weblink})**\n" +
                    $"<:src:873137640063533087> **[View run on Speedrun.com](https://speedrun.com/{thisGame.id}/run/{data.id})**",

                Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
                {
                    Url = data.game.data.assets["cover-large"].uri
                },
                Timestamp = DateTime.Parse((string)data.date),
                Color = new DiscordColor(thisGame.color)
            };

            return embed;
        }
    }
}
