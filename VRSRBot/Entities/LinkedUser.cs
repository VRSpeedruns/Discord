using System;
using System.Collections.Generic;
using System.Text;

namespace VRSRBot.Entities
{
    [Serializable]
    class LinkedUser
    {
        public ulong DiscordID;
        public string SpeedruncomID;

        public LinkedUser(ulong discord, string src)
        {
            DiscordID = discord;
            SpeedruncomID = src;
        }
    }
}
