namespace SipStack
{
    public class AckMessage : SipMessage
    {
        public AckMessage(string callId, Contact from, Contact to, int maxForwards, string supported, string via)
            : base(to, "ACK")
        {
            this.CallId = callId;
            this.From = from;
            this.MaxForwards = maxForwards;
            this.Supported = supported;
            this.Via = via;
        }
    }
}