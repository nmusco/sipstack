namespace SipStack.Isup
{
    public static class IsupExtensions
    {
        public static string ToHex(this IsupParameter parameter)
        {
            return parameter.Serialize().ToHex();
        }
    }
}