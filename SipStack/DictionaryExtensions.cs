namespace SipStack
{
    using System.Collections.Specialized;

    public static class DictionaryExtensions
    {
        public static bool TryGetValue(this OrderedDictionary dict, object key, out object val)
        {
            if (!dict.Contains(key))
            {
                val = null;
                return false;
            }

            val = dict[key];
            return true;
        }
    }
}