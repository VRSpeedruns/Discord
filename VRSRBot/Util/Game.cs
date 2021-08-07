using System;
using System.Collections.Generic;
using System.Text;

namespace VRSRBot.Util
{
    [Serializable]
    class Game
    {
        public string id;
        public string abbreviation;
        public string name;
        public string hardware;
        public string color;
        public string hoverColor;
        public IgnoredVars[] ignoredVariables;
    }

    [Serializable]
    class IgnoredVars
    {
        public string id;
        public string value;
    }
}
