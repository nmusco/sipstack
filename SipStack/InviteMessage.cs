namespace SipStack
{
    using System.Linq;

    using SipStack.Isup;

    public class InviteMessage : SipMessage
    {
        public InviteMessage(byte[] buffer)
        {
            var bodies = this.ParseBuffer(buffer).ToList();
            this.SdpData = bodies.OfType<Sdp>().FirstOrDefault();
            this.IsupData = bodies.OfType<IsupInitialAddress>().FirstOrDefault();
        }

        public InviteMessage(string callId, Contact to, Contact from, Contact contact)
            : base(to, "INVITE")
        {
            this.Headers["Allow"] = "INVITE,BYE,REGISTER,ACK,OPTIONS,CANCEL,INFO,SUBSCRIBE,NOTIFY,REFER,UPDATE";
            this.Headers["Call-ID"] = callId;
            this.Headers["Contact"] = contact;
            this.Headers["Content-Type"] = "application/xml";
            this.Headers["CSeq"] = "1 INVITE";

            this.Headers["From"] = from;
            this.Headers["Max-Forwards"] = "70";

            this.Headers["Supported"] = "100rel,timer,replaces";
            this.Headers["To"] = this.To;
            this.Headers["Via"] = string.Empty;
        }

        public Sdp SdpData { get; set; }

        public IsupInitialAddress IsupData { get; set; }

        public Contact From
        {
            get
            {
                var h = this.Headers["From"];
                if (h is string)
                {
                    return (Contact)(this.Headers["From"] = Contact.Parse(h.ToString()));
                }

                return (Contact)h;
            }
        }

        public Contact Contact
        {
            get
            {
                var h = this.Headers["Contact"];
                if (h is string)
                {
                    return (Contact)(this.Headers["Contact"] = Contact.Parse(h.ToString()));
                }

                return (Contact)h;
            }
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

            return this.IsupData != null ? new Body[] { this.IsupData } : new Body[0];
        }
    }
}