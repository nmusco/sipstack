namespace SipStack
{
    using SipStack.Isup;

    public class InviteMessage : SipMessage
    {
        public InviteMessage(string callId, Contact to, Contact from, Contact contact)
            : base(to, "INVITE")
        {
            this.Headers["Allow"] = "INVITE,BYE,REGISTER,ACK,OPTIONS,CANCEL,INFO,SUBSCRIBE,NOTIFY,REFER,UPDATE";
            this.Headers["Call-ID"] = callId;
            this.Headers["Contact"] = this.Contact = contact;
            this.Headers["Content-Type"] = "application/xml";
            this.Headers["CSeq"] = "1 INVITE";

            this.Headers["From"] = this.From = from;
            this.Headers["Max-Forwards"] = "70";

            this.Headers["Supported"] = "100rel,timer,replaces";
            this.Headers["To"] = this.To;
            this.Headers["Via"] = string.Empty;
            //            this.Headers["P-Asserted-Identity"] = this.From;
            //this.Headers["Accept"] = "application/sdp, application/isup";
        }

        public Sdp SdpData { get; set; }

        public IsupInitialAddress IsupData { get; set; }

        public Contact From { get; private set; }

        public Contact Contact { get; private set; }
        
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
                return new Body[] { this.SdpData };
            }

            return this.IsupData != null ? new Body[] { this.IsupData } : null;
        }
    }
}