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
            CommandsNext.RegisterCommands<CNext>();

            Client.ComponentInteractionCreated += async (s, e) =>
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                
                if (ulong.TryParse(e.Id, out ulong id))
                {
                    if (ValidRoleIds.Contains(id))
                    {
                        Console.WriteLine("valid role");

                        var member = await e.Guild.GetMemberAsync(e.User.Id);
                        var role = e.Guild.GetRole(id);

                        if (member.Roles.Any(r => r.Id == id))
                        {
                            Console.WriteLine("revoke");
                            await member.RevokeRoleAsync(role);
                        }
                        else
                        {
                            Console.WriteLine("grant");
                            await member.GrantRoleAsync(role);
                        }
                    }
                    else
                    {
                        Console.WriteLine("invalid role");
                    }
                }
                else
                {
                    Console.WriteLine("couldnt parse");
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
    }
}
