using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarFox.Interop.MISC
{
    public static class StringExtensions
    {
        public static string RemoveEscapes(this string input)
        {
            string output = "";
            for (int i = 0; i < input.Length; i++)
            {
                var cur = input[i];
                if(cur == 9) {  // \t
                    output += ' ';
                }
                else
                {
                    output += input[i];
                }
            }
            return output;
        }
        public static string NormalizeFormatting(this string input)
        {
            input = input.RemoveEscapes().TrimStart().TrimEnd();
            while (input.Contains("  ")) // recursive remove unnecessary spaces
                input = input.Replace("  ", " ");
            return input;
        }
    }
}
