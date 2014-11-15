namespace SipStack.Isup
{
    public class IsupRelease : IsupBody
    {
        public IsupRelease()
            : base(IsupMessageType.Release)
        {
        }

        public IsupRelease(ByteStream data)
            : this()
        {
        }
    }
}