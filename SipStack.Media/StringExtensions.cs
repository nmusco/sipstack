namespace SipStack.Media
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    public static class StringExtensions
    {
        public static IEnumerable<byte> ToByteArray(this string str)
        {
            if ((str.Length % 2) != 0)
            {
                throw new InvalidOperationException("string is not even");
            }

            for (var i = 0; i < str.Length; i += 2)
            {
                yield return byte.Parse(str[i].ToString() + str[i + 1], NumberStyles.AllowHexSpecifier);
            }
        }
    }
}