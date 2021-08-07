using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRSRBot.Util
{
    [Serializable]
    class Config
    {
        public string Token;
        public string Prefix;
        public ulong WRChannel;

        public Config()
        {
            Token = "";
            Prefix = "!";
            WRChannel = 0;
        }
    }
}
