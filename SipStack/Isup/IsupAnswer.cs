namespace SipStack.Isup
{
    public class IsupAnswer : IsupBody
    {
        public IsupAnswer()
            : base(IsupMessageType.Answer)
        {
        }

        public IsupAnswer(ByteStream data)
            : this()
        {
        }
    }
}