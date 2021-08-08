using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
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
    class Commands : BaseCommandModule
    {
        [Command("run")]
        public async Task Run(CommandContext ctx, string id)
        {
            var run = new Run(id);
            await run.DownloadData();
            var embed = run.GetEmbed();

            var message = new DiscordMessageBuilder()
                .WithEmbed(embed);

            await ctx.Channel.SendMessageAsync(message);
        }
        
        [Command("rolemessage"), RequireUserId(101384280122351616)]
        public async Task RoleMessage(CommandContext ctx, [RemainingText] string input = "")
        {
            var embed = new DiscordEmbedBuilder()
                .WithDescription("**Click on the buttons below to toggle the roles that correspond to the VR setup(s) that you own/use.**" +
                "\n\nIf you try pressing any of them and see \"This interaction failed,\" the bot is offline for some reason. Ping <@101384280122351616> to fix it :)")
                .WithColor(new DiscordColor("#ff9c00"));

            if (input == "")
            {
                input = File.ReadAllText("roles.json");
            }

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

        [Command("linkmessage"), RequireUserId(101384280122351616)]
        public async Task LinkMessage(CommandContext ctx)
        {
            var embed = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor("#FD9E02"))
                .WithDescription("**Optionally, you can also link your Discord account to your Speedrun.com account.**" +
                    "\n\nDoing this will include your Discord @ (without a ping) in new world record posts. *(And probably some more stuff in the future, this message will be updated when that happens.)*" +
                    "\n\n**• Click \"link account\" below to link your account.** The bot will send you a DM." +
                    "\n**• To unlink your account, click \"unlink account\" below.**" +
                    "\n\nNOTE: This only links the two accounts in the context of this Discord bot.");

            var message = new DiscordMessageBuilder()
                .WithEmbed(embed)
                .AddComponents(new DiscordComponent[]
                {
                    new DiscordButtonComponent(ButtonStyle.Success, "srcaccount_link", "Link Account"),
                    new DiscordButtonComponent(ButtonStyle.Danger, "srcaccount_unlink", "Unlink Account")
                });
            
            await ctx.Channel.SendMessageAsync(message);
        }
    }

    public class RequireUserIdAttribute : CheckBaseAttribute
    {
        public ulong UserId;

        public RequireUserIdAttribute(ulong userId)
        {
            this.UserId = userId;
        }

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool _)
        {
            return Task.FromResult(ctx.User.Id == UserId);
        }
    }
}
