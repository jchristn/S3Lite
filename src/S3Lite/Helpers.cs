using System;
using System.Collections.Generic;
using System.Text;

namespace S3Lite
{
    internal class Helpers
    {
        internal static string UriEncode(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == '-' || c == '~' || c == '.')
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append('%' + ((int)c).ToString("X2"));
                }
            }
            return sb.ToString();
        }

    }
}
