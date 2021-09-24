using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VRSRBot.Util;
using VRSRBot.Core;

namespace VRSRBot.Core
{
    class Heartbeat
    {
        private static GitHubClient client;
        private static bool lastHeartbeat = true;

        public static void Start(Credentials creds)
        {
            client = new GitHubClient(new ProductHeaderValue("Discord-Heartbeat"));
            client.Credentials = creds;

            Thread thread = new Thread(Loop);
            thread.Start();
        }

        static void Loop()
        {
            AsyncLoop().GetAwaiter().GetResult();
        }

        static async Task AsyncLoop()
        {
            while (true)
            {
                var time = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
                var release = new ReleaseUpdate();
                release.Body = time;

                MiscMethods.Log($"&3Sending heartbeat.");
                try
                {
                    await client.Repository.Release.Edit("VRSRBot", "Heartbeats", 49565635, release); // 49565635 = discord's release
                    MiscMethods.Log($"&3Heartbeat sent.");

                    if (!lastHeartbeat)
                    {
                        MiscMethods.Log("Attempting to reconnect...", "&3");
                        try
                        {
                            await Bot.Client.DisconnectAsync();
                            Bot.Init(false);
                            MiscMethods.Log("Successfully reconnected.", "&3");
                            lastHeartbeat = true;
                        }
                        catch
                        {
                            MiscMethods.Log("Couldn't reconnect.", "&c");
                            lastHeartbeat = false;
                        }
                    }
                }
                catch (Exception e)
                {
                    MiscMethods.Log($"&cHeartbeat not sent.\n&7- \"{e.Message}\"");
                    lastHeartbeat = false;
                }

                await Task.Delay(300000); // 5 minutes
            }
        }
    }
}
