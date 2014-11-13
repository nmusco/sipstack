namespace SipStack
{

    using SipStack.Isup;

    public class InviteMessage : SipMessage
    {
        public Sdp SdpData { get; set; }

        public IsupInitialAddress IsupData { get; set; }
        public Contact From { get; private set; }

        public Contact Contact { get; private set; }
        public InviteMessage(string callId, Contact to, Contact from, Contact contact)
            : base(to, "INVITE")
        {
            this.Headers["To"] = this.To;
            this.Headers["Call-ID"] = callId;

            this.Headers["From"] = this.From = from;
            this.Headers["CSeq"] = "1 INVITE";
            this.Headers["Max-Forwards"] = "70";
            this.Headers["Contact"] = this.Contact = contact;

            this.Headers["Allow"] = string.Join(", ", new[] { "INVITE", "ACK", "PRACK", "CANCEL", "BYE", "OPTIONS" });

            this.Headers["Supported"] = "100rel";
            this.Headers["P-Asserted-Identity"] = this.From;
            this.Headers["Accept"] = "application/sdp, application/isup";
        }

        public override void Deserialize(byte[] data)
        {
            throw new System.NotImplementedException();
        }

        protected override Body[] GetBodies()
        {
            if (this.IsupData != null && this.SdpData != null)
            {
                return new Body[] { this.SdpData, this.IsupData };
            }

            if (this.SdpData != null)
            {
                return new[] { this.SdpData };
            }

            if (this.IsupData != null)
            {
                return new[] { this.IsupData };
            }

            return null;
        }
    }
}