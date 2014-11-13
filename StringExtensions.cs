namespace SipStack
{
    using System.IO;

    public static class StringExtensions
    {
        public static string SafeEmpty(this string str)
        {
            return string.IsNullOrWhiteSpace(str) ? string.Empty : str;
        }

        public static string ReadBlock(this TextReader reader, int start, int length)
        {
            var cha = new char[length];
            reader.ReadBlock(cha, start, length);
            return new string(cha);
        }
    }
}