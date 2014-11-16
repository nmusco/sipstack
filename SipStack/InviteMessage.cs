namespace SipStack
{
    using System.Linq;

    using SipStack.Isup;

    public sealed class InviteMessage : SipMessage
    {
        public InviteMessage(byte[] buffer)
        {
            this.ParseBuffer(buffer);
        }

        public InviteMessage(string callId, Contact to, Contact from, Contact contact)
            : base(to, "INVITE")
        {
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
    }
}