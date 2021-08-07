using System;
using System.Linq;

namespace VRSRBot.Util
{
    class FConsole
    {
        private static readonly char[] colors = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F' };

        // & - text color
        // % - background color

        public static void Write(string input)
        {
            var fore = Console.ForegroundColor;
            var back = Console.BackgroundColor;
            if (!input.Contains("&") && !input.Contains("%"))
            {
                Console.Write(input);
                return;
            }

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c != '&' && c != '%')
                {
                    if (!colors.Contains(c) || input[i - 1] != '&' && input[i - 1] != '%')
                        Console.Write(c);
                }
                else if (c == '&')
                {
                    if (i == input.Length - 1 || !colors.Contains(input[i + 1]))
                        Console.Write(c);
                    else if (colors.Contains(input[i + 1]))
                        Console.ForegroundColor = charToColor(input[i + 1]);
                }
                else if (c == '%')
                {
                    if (i == input.Length - 1 || !colors.Contains(input[i + 1]))
                        Console.Write(c);
                    else if (colors.Contains(input[i + 1]))
                        Console.BackgroundColor = charToColor(input[i + 1]);
                }
            }
            Console.ForegroundColor = fore;
            Console.BackgroundColor = back;
        }
        public static void WriteLine(string input)
        {
            Write(input + "\n");
        }
        private static ConsoleColor charToColor(char c)
        {
            switch (c)
            {
                case '0': return ConsoleColor.Black;
                case '1': return ConsoleColor.DarkBlue;
                case '2': return ConsoleColor.DarkGreen;
                case '3': return ConsoleColor.DarkCyan;
                case '4': return ConsoleColor.DarkRed;
                case '5': return ConsoleColor.DarkMagenta;
                case '6': return ConsoleColor.DarkYellow;
                case '7': return ConsoleColor.Gray;
                case '8': return ConsoleColor.DarkGray;
                case '9': return ConsoleColor.Blue;
                case 'a': return ConsoleColor.Green;
                case 'b': return ConsoleColor.Cyan;
                case 'c': return ConsoleColor.Red;
                case 'd': return ConsoleColor.Magenta;
                case 'e': return ConsoleColor.Yellow;
                case 'f': return ConsoleColor.White;
            }
            return ConsoleColor.White;
        }
    }
}
