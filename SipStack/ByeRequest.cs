namespace SipStack
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class ByeRequest : SipMessage
    {
        private IEnumerable<Body> bodies = new List<Body>();

        public override void Deserialize(byte[] buffer)
        {
            var ms = new MemoryStream(buffer);

            var reader = new StreamReader(ms);
            
            this.ParseRequestLine(reader.ReadLine());

            do
            {
                var currentline = reader.ReadLine();
                if (currentline == null)
                {
                    break;
                }

                if (currentline == string.Empty)
                {
                    if (this.Headers.Contains("Content-Type") && this.Headers.Contains("Content-Length"))
                    {
                        var body = new byte[int.Parse(this.Headers["Content-Length"].ToString())];
                        ms.Seek(-body.Length, SeekOrigin.End);
                        ms.Read(body, 0, body.Length);
                        this.bodies = SipResponse.BodyParser.Parse(this.Headers["Content-Type"].ToString(), body).ToList();
                        break;
                    }
                }
                else
                {
                    this.ParseHeader(currentline);
                }
            }
            while (true);
        }
    }
}