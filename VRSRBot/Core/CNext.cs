using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRSRBot.Util;

namespace VRSRBot.Core
{
    class CNext : BaseCommandModule
    {
        [Command("run")]
        public async Task Run(CommandContext ctx, string id)
        {
            var run = new Run(id);
            await run.DownloadData();
            var embed = run.GetEmbed();

            var message = new DiscordMessageBuilder()
                .WithEmbed(embed);

            await ctx.RespondAsync(message);
        }

        [Command("rolemessage")]
        public async Task RoleMessage(CommandContext ctx, [RemainingText] string input)
        {
            var embed = new DiscordEmbedBuilder()
                .WithDescription("**Click on the buttons below to toggle the roles that correspond to the VR setup(s) that you own/use.**" +
                "\n\nIf you try pressing any of them and see \"This interaction failed,\" the bot is offline for some reason. Ping <@101384280122351616> to fix it :)")
                .WithColor(new DiscordColor("#ff9c00"));

            if (input.StartsWith('`') && input.EndsWith('`'))
            {
                input = input.Substring(1, input.Length - 2);
            }
            dynamic json = JsonConvert.DeserializeObject(input);

            File.WriteAllText("roles.json", JsonConvert.SerializeObject(json, Formatting.Indented));

            var message = new DiscordMessageBuilder()
                .WithEmbed(embed);

            foreach (var component in Bot.GetRoleButtons(json))
            {
                message.AddComponents(component);
            }

            await ctx.Channel.SendMessageAsync(message);
        }
    }
}
