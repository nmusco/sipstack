namespace SipStack
{
    public static class SipMessageExtensions
    {
        public static void CopyHeadersFrom(this SipMessage msg, SipMessage @from, params string[] headers)
        {
            if (headers == null)
            {
                foreach (var h in @from.Headers)
                {
                    msg.Headers[h] = @from.Headers[h];
                }
            }
            else
            {
                var headersToCopy = new[] { "Call-ID", "From", "Max-Forwards", "Supported", "To", "Via" };
                foreach (var c in headersToCopy)
                {
                    msg.Headers[c] = @from.Headers[c];
                }
            }
        }
    }
}