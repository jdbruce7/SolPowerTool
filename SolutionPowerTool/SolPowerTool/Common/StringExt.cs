using System;
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
    }
}