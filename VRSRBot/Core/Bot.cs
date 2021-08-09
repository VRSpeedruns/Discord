using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Net;
using DSharpPlus.Net.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VRSRBot.Util;
using VRSRBot.Entities;

namespace VRSRBot.Core
{
    class Bot
    {
        public static Config Config;

        public static DiscordClient Client;
        public static InteractivityExtension Interactivity;
        public static CommandsNextExtension CommandsNext;

        public static List<ulong> ValidRoleIds;
        public static List<LinkedUser> LinkedUsers;

        public static List<ulong> UsersCurrentlyLinking = new List<ulong>();
        public static List<ulong> UsersConfirmingLink = new List<ulong>();

        public Bot(Config cfg)
        {
            Program.Log("Initializing Bot...", "&3");
            Config = cfg;

            if (File.Exists("files/roles.json"))
            {
                GetRoleButtons(JsonConvert.DeserializeObject(File.ReadAllText("files/roles.json")));
            }
            else
            {
                ValidRoleIds = new List<ulong>();
            }

            if (File.Exists("files/linkedusers.json"))
            {
                LinkedUsers = JsonConvert.DeserializeObject<List<LinkedUser>>(File.ReadAllText("files/linkedusers.json"));
            }
            else
            {
                LinkedUsers = new List<LinkedUser>();
            }

            var config = new DiscordConfiguration
            {
                Token = Config.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Critical
            };

            Program.Log("Initializing components...", "&3");
            Client = new DiscordClient(config);
            Interactivity = Client.UseInteractivity(new InteractivityConfiguration { Timeout = new TimeSpan(0, 1, 30) });
            CommandsNext = Client.UseCommandsNext(new CommandsNextConfiguration
            {
                EnableDefaultHelp = false,
                IgnoreExtraArguments = true,
                StringPrefixes = new[] { Program.Config.Prefix }
            });
            CommandsNext.RegisterCommands<Commands>();
            
            Client.ComponentInteractionCreated += async (s, e) =>
            {
                if (e.Id.StartsWith("srcaccount_"))
                {
                    HandleAccountLink(s, e);
                }
                else
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                    if (ulong.TryParse(e.Id, out ulong id))
                    {
                        if (ValidRoleIds.Contains(id))
                        {
                            var member = await e.Guild.GetMemberAsync(e.User.Id);
                            var role = e.Guild.GetRole(id);

                            if (member.Roles.Any(r => r.Id == id))
                            {
                                await member.RevokeRoleAsync(role);
                            }
                            else
                            {
                                await member.GrantRoleAsync(role);
                            }
                        }
                    }
                }
            };

            Client.Ready += async (s, e) =>
            {
                await Client.UpdateStatusAsync(new DiscordActivity("VRSpeed.run", ActivityType.Watching), UserStatus.Online);
            };

            Program.Log("Bot initialization complete. Connecting...", "&3");

            Client.ConnectAsync();

            Program.Log("Connected.", "&3");
        }

        public static List<List<DiscordComponent>> GetRoleButtons(dynamic json)
        {
            ValidRoleIds = new List<ulong>();

            var components = new List<List<DiscordComponent>>();

            foreach (var row in json)
            {
                var buttons = new List<DiscordComponent>();
                foreach (var button in row.row)
                {
                    buttons.Add(new DiscordButtonComponent(ButtonStyle.Secondary, (string)button.role, (string)button.name));

                    var role = ulong.Parse((string)button.role);
                    if (!ValidRoleIds.Contains(role))
                    {
                        ValidRoleIds.Add(role);
                    }
                }
                components.Add(buttons);
            }

            return components;
        }

        public static async Task HandleAccountLink(DiscordClient s, DSharpPlus.EventArgs.ComponentInteractionCreateEventArgs e)
        {
            if (e.Id == "srcaccount_link")
            {
                DiscordEmbedBuilder embed;
                DiscordInteractionResponseBuilder message;

                if (LinkedUsers.Any(u => u.DiscordID == e.User.Id))
                {
                    embed = new DiscordEmbedBuilder()
                        .WithColor(new DiscordColor(Config.ErrorColor))
                        .WithDescription("Error: Your account is already linked." +
                            "\nPlease unlink your account before attempting to link a new account.");

                    message = new DiscordInteractionResponseBuilder()
                        .AsEphemeral(true)
                        .AddEmbed(embed);

                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, message);

                    return;
                }
                else if (UsersCurrentlyLinking.Contains(e.User.Id) || UsersConfirmingLink.Contains(e.User.Id))
                {
                    embed = new DiscordEmbedBuilder()
                        .WithColor(new DiscordColor(Config.ErrorColor))
                        .WithDescription("Error: You're already linking your account.");

                    message = new DiscordInteractionResponseBuilder()
                        .AsEphemeral(true)
                        .AddEmbed(embed);

                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, message);

                    return;
                }

                DiscordChannel dm = null;

                try
                {
                    var member = await e.Guild.GetMemberAsync(e.User.Id);
                    dm = await member.CreateDmChannelAsync();

                    embed = new DiscordEmbedBuilder()
                        .WithColor(new DiscordColor(Config.PrimaryColor))
                        .WithDescription("Hello! Follow the steps below to link your Speedrun.com account to your Discord account." +
                            "\n\n**NOTE: This only links the two accounts in the context of this Discord bot.**");

                    await dm.SendMessageAsync(embed);
                }
                catch (DSharpPlus.Exceptions.UnauthorizedException)
                {
                    embed = new DiscordEmbedBuilder()
                        .WithColor(new DiscordColor(Config.ErrorColor))
                        .WithDescription("Error: I don't have permission to DM you!" +
                            "\n\nPlease right click on the server icon and enable:" +
                            "\n`Privacy Settings -> Allow direct messages from server members.`");

                    message = new DiscordInteractionResponseBuilder()
                        .AsEphemeral(true)
                        .AddEmbed(embed);

                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, message);

                    return;
                }

                await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                await dm.TriggerTypingAsync();

                using (WebClient wc = new WebClient())
                {
                    await LinkAccount(wc, dm, e.User.Id);
                }

                if (UsersCurrentlyLinking.Contains(e.User.Id))
                {
                    embed = new DiscordEmbedBuilder()
                       .WithColor(new DiscordColor(Config.ErrorColor))
                       .WithDescription("Error: The request timed out." +
                           "\n\nIf you'd still like to link your account, press the \"Link Account\" button again.");

                    try
                    {
                        await dm.SendMessageAsync(embed);
                    }
                    catch
                    {
                        //
                    }

                    UsersCurrentlyLinking.Remove(e.User.Id);
                }
            }
            else if (e.Id == "srcaccount_unlink")
            {
                DiscordEmbedBuilder embed;
                DiscordInteractionResponseBuilder message;

                if (!LinkedUsers.Any(u => u.DiscordID == e.User.Id))
                {
                    embed = new DiscordEmbedBuilder()
                        .WithColor(new DiscordColor(Config.ErrorColor))
                        .WithDescription("Error: You don't have an account linked.");

                    message = new DiscordInteractionResponseBuilder()
                        .AsEphemeral(true)
                        .AddEmbed(embed);

                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, message);

                    return;
                }

                LinkedUsers.RemoveAll(u => u.DiscordID == e.User.Id);
                File.WriteAllText("files/linkedusers.json", JsonConvert.SerializeObject(LinkedUsers, Formatting.Indented));

                embed = new DiscordEmbedBuilder()
                    .WithColor(new DiscordColor(Config.PrimaryColor))
                    .WithDescription("Account successfully unlinked.");

                message = new DiscordInteractionResponseBuilder()
                    .AsEphemeral(true)
                    .AddEmbed(embed);

                await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, message);
            }
            else if (e.Id == "srcaccount_link_confirm")
            {
                if (!UsersConfirmingLink.Contains(e.User.Id))
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                    return;
                }

                var desc = e.Message.Embeds[0].Description.Replace("**Account found!**", "").Replace("\n\n**Is this you?**", "");

                var srcId = desc.Split("(ID: ")[1].Split(")")[0];

                LinkedUsers.Add(new LinkedUser(e.User.Id, srcId));
                File.WriteAllText("files/linkedusers.json", JsonConvert.SerializeObject(LinkedUsers, Formatting.Indented));

                var embed = new DiscordEmbedBuilder()
                    .WithColor(new DiscordColor(Config.PrimaryColor))
                    .WithThumbnail(e.Message.Embeds[0].Thumbnail.Url.ToString())
                    .WithDescription($"**Account successfully linked!**{desc}");

                UsersConfirmingLink.Remove(e.User.Id);

                try
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(embed));
                }
                catch { }
            }
            else if (e.Id == "srcaccount_link_deny")
            {
                if (!UsersConfirmingLink.Contains(e.User.Id))
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                    return;
                }

                var embed = new DiscordEmbedBuilder()
                       .WithColor(new DiscordColor(Config.ErrorColor))
                       .WithDescription("Account linking cancelled." +
                           "\n\nIf you'd still like to link your account, press the \"Link Account\" button again.");

                UsersConfirmingLink.Remove(e.User.Id);

                try
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(embed));
                }
                catch { }
            }
        }

        public static async Task LinkAccount(WebClient wc, DiscordChannel dm, ulong userId)
        {
            UsersCurrentlyLinking.Add(userId);

            string id, data;
            dynamic json;
            int count;

            do
            {
                id = "VRSR-" + MiscMethods.GenerateID();
                
                data = await SRCAPICall($"https://www.speedrun.com/api/v1/users?speedrunslive=VRSR-{id}", wc);
                
                try
                {
                    json = JsonConvert.DeserializeObject(data);
                }
                catch
                {
                    var embedError = new DiscordEmbedBuilder()
                        .WithColor(new DiscordColor(Config.ErrorColor))
                        .WithDescription("Error: The Speedrun.com API returned an error." +
                            $"\n```{data}```");

                    UsersCurrentlyLinking.Remove(userId);

                    try
                    {
                        await dm.SendMessageAsync(embedError);
                    }
                    catch { }

                    return;
                }

                count = (json.data).Count;
            }
            while (count != 0);

            var embed = new DiscordEmbedBuilder()
               .WithColor(new DiscordColor(Config.PrimaryColor))
               .WithDescription("On your Speedrun.com profile, set the SpeedRunsLive social field to the following code:" +
               $"\n\n**```{id}```**" +
               $"\nThis code will be active for the next two minutes." +
               $"\n\n*(You can set it back to what it was originally after this process is complete.)*" +
               $"\n\nExample:")
               .WithImageUrl("https://vrspeed.run/assets/images/link_account_srl.png");

            try
            {
                await dm.SendMessageAsync(embed);
            }
            catch
            {
                UsersCurrentlyLinking.Remove(userId);
                return;
            }

            for (var i = 0; i < 12; i++) //2 minutes total, check once every 10 seconds
            {
                await Task.Delay(10000);

                data = await SRCAPICall($"https://www.speedrun.com/api/v1/users?speedrunslive={id}&max={i + 1}", wc);
                json = JsonConvert.DeserializeObject(data);

                if ((json.data).Count > 0)
                {
                    embed = new DiscordEmbedBuilder()
                        .WithColor(new DiscordColor(Config.PrimaryColor))
                        .WithThumbnail($"https://vrspeed.run/vrsrassets/php/userIcon.php?t=p&u={json.data[0].names.international}")
                        .WithDescription("**Account found!**" +
                            $"\n\nName: {json.data[0].names.international} (ID: {json.data[0].id})" +
                            $"\nLink: {json.data[0].weblink}" +
                            $"\n\n**Is this you?**");

                    var message = new DiscordMessageBuilder()
                        .WithEmbed(embed)
                        .AddComponents(new DiscordComponent[]
                        {
                            new DiscordButtonComponent(ButtonStyle.Success, "srcaccount_link_confirm", "Confirm"),
                            new DiscordButtonComponent(ButtonStyle.Danger, "srcaccount_link_deny", "Deny")
                        });

                    DiscordMessage msg;

                    try
                    {
                        msg = await dm.SendMessageAsync(message);
                    }
                    catch
                    {
                        UsersCurrentlyLinking.Remove(userId);
                        return;
                    }

                    UsersConfirmingLink.Add(userId);
                    UsersCurrentlyLinking.Remove(userId);

                    for (var k = 0; k < 12; k++) //another 2 minutes to confirm/deny
                    {
                        await Task.Delay(10000);
                        if (!UsersConfirmingLink.Contains(userId))
                            return;
                    }
                    
                    embed = new DiscordEmbedBuilder()
                       .WithColor(new DiscordColor(Config.ErrorColor))
                       .WithDescription("Error: The request timed out." +
                           "\n\nIf you'd still like to link your account, press the \"Link Account\" button again.");

                    message = new DiscordMessageBuilder()
                        .WithEmbed(embed);

                    UsersConfirmingLink.Remove(userId);

                    try
                    {
                        await msg.ModifyAsync(message);
                    }
                    catch { }

                    return;
                }
            }
        }
        public static async Task<string> SRCAPICall(string url, WebClient wc)
        {
            string result;
            try
            {
                result = await wc.DownloadStringTaskAsync(url);
            }
            catch (Exception e)
            {
                result = e.Message;
            }
            return result;
        }
    }
}
