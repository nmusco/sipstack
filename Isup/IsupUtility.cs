namespace SipStack.Isup
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    public static class IsupUtility
    {
        internal static string UnReverseNumber(byte[] buffer, int start, bool isOdd)
        {
            var sb = new StringBuilder();
            for (var i = start; i < buffer.Length; i++)
            {
                sb.Append(buffer[i].ToString("D2"));
            }

            var str = sb.ToString();
            if (!isOdd)
            {
                return str;
            }

            var lastDigit = str.Substring(str.Length - 2);

            return str.Substring(0, str.Length - 2) + lastDigit[0];
        }

        internal static IEnumerable<byte> GetReversedNumber(string number)
        {
            var isEven = (number.Length % 2) == 0;
            for (var i = 0; i < number.Length; i += 2)
            {
                if (!isEven && i + 1 == number.Length)
                {
                    yield return byte.Parse("0" + number[i]);
                }
                else
                {
                    var n1 = number[i].ToString(CultureInfo.InvariantCulture);
                    var n2 = number[i + 1];

                    yield return Convert.ToByte(n2 + n1, 16);
                }
            }
        }
    }
}