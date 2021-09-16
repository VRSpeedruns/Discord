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
using VRSRBot.Entities;

namespace VRSRBot.Core
{
    class Commands : BaseCommandModule
    {
        [Command("wr"), RequireUserPermissions(Permissions.ManageChannels)]
        public async Task WR(CommandContext ctx, string id)
        {
            if (id.Contains("/"))
            {
                id = id.Split("/").Last();
            }

            var run = new Run(id);
            await run.DownloadData();
            var embed = run.GetEmbed();

            var channel = ctx.Guild.GetChannel(Program.Config.WRChannel);
            await channel.SendMessageAsync(embed);

            try { await ctx.Message.DeleteAsync(); } catch { }
        }
        
        [Command("rolemessage"), RequireUserId(101384280122351616)]
        public async Task RoleMessage(CommandContext ctx, [RemainingText] string input = "")
        {
            var embed = new DiscordEmbedBuilder()
                .WithDescription("**Click on the buttons below to toggle the roles that correspond to the VR setup (or setups) that you own/use.**" +
                "\n\nIf you try pressing any of them and see \"This interaction failed,\" the bot has gone offline for some reason. Please check the **[status page](https://vrspeed.run/status)** to confirm the bot is down, and ping <@101384280122351616> to fix it. :)")
                .WithColor(new DiscordColor(Program.Config.PrimaryColor));

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
                .WithColor(new DiscordColor(Bot.Config.PrimaryColor))
                .WithDescription("**Optionally, you can link your Discord account to your Speedrun.com account.**" +
                    "\n\nDoing this will include your Discord @username (without a ping) in new world record posts (+ more stuff in the future)." +
                    "\n\n**• Click \"Link Account\" below to link your account.** The bot will send you a DM." +
                    "\n**• To unlink your account, click \"Unlink Account\" below.**" +
                    "\n\nNOTE: This only links the two accounts in the context of this Discord bot." +
                    "\n\nIf you try pressing either of the buttons and you see \"This interaction failed,\" the bot has gone offline for some reason. Please check the **[status page](https://vrspeed.run/status)** to confirm the bot is down, and ping <@101384280122351616> to fix it. :)");

            var message = new DiscordMessageBuilder()
                .WithEmbed(embed)
                .AddComponents(new DiscordComponent[]
                {
                    new DiscordButtonComponent(ButtonStyle.Success, "srcaccount_link", "Link Account"),
                    new DiscordButtonComponent(ButtonStyle.Danger, "srcaccount_unlink", "Unlink Account")
                });
            
            await ctx.Channel.SendMessageAsync(message);
        }

        [Command("config"), RequireUserId(101384280122351616)]
        public async Task Config(CommandContext ctx, string setting = "", string input = "")
        {
            if (setting == "" || input == "")
            {
                var embed = new DiscordEmbedBuilder()
                {
                    Description = $"• 'prefix': `{Program.Config.Prefix}`\n" +
                                  $"• 'primaryColor': `{Program.Config.PrimaryColor}`\n" +
                                  $"• 'errorColor': `{Program.Config.ErrorColor}`\n" +
                                  $"• 'wrChannel': `{Program.Config.WRChannel}`\n"
                };
                await ctx.Channel.SendMessageAsync(embed);
                return;
            }
            
            if (setting.ToLower() == "prefix")
            {
                Program.Config.Prefix = input;
            }
            else if (setting.ToLower() == "primarycolor")
            {
                Program.Config.PrimaryColor = input;
            }
            else if (setting.ToLower() == "errorcolor")
            {
                Program.Config.ErrorColor = input;
            }
            else if (setting.ToLower() == "wrchannel")
            {
                Program.Config.WRChannel = ulong.Parse(input);
            }
            else { return; }

            File.WriteAllText("files/config.json", JsonConvert.SerializeObject(Program.Config, Formatting.Indented));
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromGuildEmote(ctx.Client, 665860688463396864));
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
