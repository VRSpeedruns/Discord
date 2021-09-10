﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRSRBot.Util
{
    class MiscMethods
    {
        private static Random random = new Random();

        public static string GenerateID()
        {
            const string chars = "BCDFGHJKLMNPRSTVWXYZ23456789";
            return new string(Enumerable.Repeat(chars, 8)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static uint Epoch(DateTime time)
        {
            return (uint)Math.Floor((time - new DateTime(1970, 1, 1)).TotalSeconds);
        }
    }
}
