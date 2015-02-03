namespace SipStack
{
    using System.IO;

    public class OkResponse : SipMessage
    {
        public OkResponse(Contact to)
        {
            this.Headers["To"] = to;
        }

        protected override void SerializeRequestLine(TextWriter writer)
        {
            writer.WriteLine("SIP/2.0 200 OK");
        }
    }
}