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
                }
                catch
                {
                    MiscMethods.Log($"&cHeartbeat not sent.");
                }


                await Task.Delay(300000); // 5 minutes
            }
        }
    }
}
