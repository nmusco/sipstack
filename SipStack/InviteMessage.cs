namespace SipStack
{
    using System.IO;
    using System.Linq;

    using SipStack.Isup;

    public class InviteMessage : SipMessage
    {
        public InviteMessage(byte[] buffer)
        {
            var bs = new ByteStream(buffer, 0);
            this.ParseRequestLine(bs.ReadLine());
            foreach (var l in bs.Lines())
            {
                if (l == string.Empty)
                {
                    var bodies = SipResponse.BodyParser.Parse(
                        this.Headers["Content-Type"].ToString(),
                        bs.Read(bs.Length - bs.Position)).ToList();
                    this.SdpData = bodies.OfType<Sdp>().First();
                    this.IsupData = bodies.OfType<IsupInitialAddress>().First();
                    break;
                }
                else
                {
                    this.ParseHeader(l);
                }
            }
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
            //            this.Headers["P-Asserted-Identity"] = this.From;
            //this.Headers["Accept"] = "application/sdp, application/isup";
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