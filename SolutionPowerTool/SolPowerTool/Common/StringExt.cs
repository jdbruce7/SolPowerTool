using System;
using System.Linq;
using System.Text;

namespace SolPowerTool.App.Common
{
    public static class StringExt
    {
        public static string Esacpe(this string text)
        {
            var sb = new StringBuilder();
            foreach (char c in text)
                if (" abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".IndexOf(c) >= 0)
                    sb.Append(c);
                else
                    sb.Append(Uri.HexEscape(c));
            return sb.ToString();
        }

        public static bool IsInList(this string text,
            StringComparison stringComparison,
            params string[] args)
        {
            return args.Any(item => string.Compare(text, item, stringComparison) == 0);
        }
        public static bool IsInList(this string text,
            params string[] args)
        {
            return args.Any(item => string.Compare(text, item, StringComparison.InvariantCultureIgnoreCase) == 0);
        }
    }
}