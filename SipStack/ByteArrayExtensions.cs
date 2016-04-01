namespace SipStack
{
    using System;
    using System.Collections.Generic;

    public static class ByteArrayExtensions
    {
        public static int AsShort(this byte[] data)
        {
            return BitConverter.ToInt16(data, 0);
        }

        public static IEnumerable<byte[]> AsPairs(this byte[] data)
        {
            for (var startPos = 0; startPos < data.Length; startPos += 2)
            {
                yield return new byte[] { data[startPos], data[startPos + 1] };
            }
        }
    }
}