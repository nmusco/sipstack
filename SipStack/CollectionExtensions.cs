namespace SipStack
{
    using System.Collections.Generic;

    internal static class CollectionExtensions
    {
        public static IList<KeyValuePair<T, U>> Add<T, U>(this IList<KeyValuePair<T, U>> list, T key, U value)
        {
            list.Add(new KeyValuePair<T, U>(key, value));
            return list;
        }
    }
}