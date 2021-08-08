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

namespace VRSRBot.Core
{
    class Bot
    {
        public Config Config;

        public DiscordClient Client;
        public InteractivityExtension Interactivity;
        public CommandsNextExtension CommandsNext;

        public static List<ulong> ValidRoleIds;

        public Bot(Config cfg)
        {
            Program.Log("Initializing Bot...", "&3");
            Config = cfg;

            if (File.Exists("roles.json"))
            {
                GetRoleButtons(JsonConvert.DeserializeObject(File.ReadAllText("roles.json")));
            }
            else
            {
                ValidRoleIds = new List<ulong>();
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
                    await HandleAccountLink(s, e);
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
                DiscordChannel dm = null;

                try
                {
                    var member = await e.Guild.GetMemberAsync(e.User.Id);
                    dm = await member.CreateDmChannelAsync();

                    var embed = new DiscordEmbedBuilder()
                        .WithColor(new DiscordColor("#FD9E02"))
                        .WithDescription("Hello! Follow the steps below to link your Speedrun.com account to your Discord account." +
                            "\n\n**NOTE: This only links the two accounts in the context of this Discord bot.**");

                    await dm.SendMessageAsync(embed);
                }
                catch (DSharpPlus.Exceptions.UnauthorizedException)
                {
                    var embedError = new DiscordEmbedBuilder()
                        .WithColor(new DiscordColor("#F14668"))
                        .WithDescription("Error: I don't have permission to DM you!" +
                            "\n\nPlease right click on the server icon and enable:" +
                            "\n`Privacy Settings -> Allow direct messages from server members.`");

                    var message = new DiscordInteractionResponseBuilder()
                        .AsEphemeral(true)
                        .AddEmbed(embedError);

                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, message);

                    return;
                }
                
                await dm.TriggerTypingAsync();

                using (WebClient wc = new WebClient())
                {
                    var success = await LinkAccount(wc, dm);

                    if (!success)
                    {
                        //account wasnt linked successfully
                    }
                }
            }
            else if (e.Id == "srcaccount_unlink")
            {
                //TODO
            }
        }

        public static async Task<bool> LinkAccount(WebClient wc, DiscordChannel dm)
        {
            var id = "";
            var data = "";
            dynamic json = null;
            var count = 0;

            do
            {
                id = "VRSR-" + MiscMethods.GenerateID();

                try
                {
                    data = await wc.DownloadStringTaskAsync($"https://www.speedrun.com/api/v1/users?speedrunslive=VRSR-{id}");
                }
                catch (WebException e)
                {
                    Console.WriteLine(e.Message);
                }

                json = JsonConvert.DeserializeObject(data);
                count = (json.data).Count;
            }
            while (count != 0);

            var embed = new DiscordEmbedBuilder()
               .WithColor(new DiscordColor("#FD9E02"))
               .WithDescription("On your Speedrun.com profile, set the SpeedRunsLive social field to the following code:" +
               $"\n*(You can set it back to what it was originally after this process is complete.)*" +
               $"\n\nThis code will be available for two minutes." +
               $"\n\n**```{id}```**\nExample:")
               .WithImageUrl("https://vrspeed.run/assets/images/link_account_srl.png");

            await dm.SendMessageAsync(embed);

            for (var i = 0; i < 12; i++) //2 minutes total, check once every 10 seconds
            {
                await Task.Delay(10000);

                data = await wc.DownloadStringTaskAsync($"https://www.speedrun.com/api/v1/users?speedrunslive={id}&max={i + 1}");
                json = JsonConvert.DeserializeObject(data);

                if ((json.data).Count > 0)
                {
                    //temp response
                    embed = new DiscordEmbedBuilder()
                        .WithColor(new DiscordColor("#FD9E02"))
                        .WithDescription("**Account found!**" +
                            $"\n\nName: {json.data[0].names.international}");

                    await dm.SendMessageAsync(embed);

                    //TODO: save the linked accounts (src user id and discord user id)

                    return true;
                }
            }

            return false;
        }
    }
}
