using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRSRBot.Entities
{
    [Serializable]
    class Config
    {
        public string Token;
        public string Prefix;
        public ulong WRChannel;

        public string PrimaryColor;
        public string ErrorColor;

        public Config()
        {
            Token = "";
            Prefix = "!";
            WRChannel = 0;

            PrimaryColor = "#FD9E02";
            ErrorColor = "#F14668";
        }
    }
}
