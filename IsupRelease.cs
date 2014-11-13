namespace SipStack.Isup
{
    public class IsupRelease : IsupBody
    {
        protected override IsupMessageType Type
        {
            get
            {
                return IsupMessageType.Release;
            }
        }

        public IsupRelease()
        {
            
        }

        public IsupRelease(ByteStream data)
        {
            
        }
    }
}