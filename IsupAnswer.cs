namespace SipStack.Isup
{
    public class IsupAnswer : IsupBody
    {
        protected override IsupMessageType Type
        {
            get
            {
                return IsupMessageType.IAM;
            }
        }

        public IsupAnswer() { }

        public IsupAnswer(ByteStream data) { }
    }
}