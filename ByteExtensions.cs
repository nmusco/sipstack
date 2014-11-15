namespace SipStack
{
    using System.Collections.Generic;
    using System.Linq;

    public static class ByteExtensions
    {
        public static string ToHex(this IEnumerable<byte> data)
        {
            return data.Aggregate(string.Empty, (initial, next) => initial + next.ToString("X2"));
        }
    }
}