namespace SipStack
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    public abstract class SipMessage
    {
        private const string BoundaryId = "unique-boundary-1";
        public Contact To { get; private set; }

        public List<KeyValuePair<string, string>> ContactParameters { get; private set; }

        public string Method { get; private set; }

        public IDictionary<string, string> Headers { get; private set; }

        public SipMessage()
        {
            this.Headers = new Dictionary<string, string>();
            
        }
        public SipMessage(string method)
            :this()
        {
            this.Method = method;
        }
        protected SipMessage(Contact to, string method)
            : this(method)
        {
            this.To = to;
            
            this.ContactParameters = new List<KeyValuePair<string, string>>();
        }

        public virtual string Serialize()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0} {1}", this.Method, this.To.ToString(false));

            sb.AppendLine(" SIP/2.0");

            var bodies = this.GetBodies();
            if (bodies.Length > 0)
            {

                this.Headers["Content-Type"] = string.Format("multipart/mixed; boundary={0}", BoundaryId);
                this.Headers["MIME-Version"] = "1.0";
            }



            var sbBody = new StringBuilder();

            if (bodies.Length > 0)
            {

                sbBody.AppendLine();
                sbBody.AppendLine(string.Format("--{0}", BoundaryId));
                foreach (var b in bodies)
                {
                    sbBody.AppendLine(string.Format("Content-Type: {0}", b.ContentType));
                    foreach (var kvp in b.Headers)
                    {
                        sbBody.AppendLine(string.Format("{0}: {1}", kvp.Key, kvp.Value));
                    }
                    sbBody.AppendLine();
                    sbBody.AppendLine(b.ContentText);
                    sbBody.AppendLine(string.Format("--{0}", BoundaryId));
                }
                sbBody.Length = sbBody.Length - 2;
                sbBody.AppendLine("--");
                this.Headers["Content-Length"] = sbBody.Length.ToString();

            }

            foreach (var kvp in this.Headers)
            {
                sb.AppendLine(string.Format("{0}: {1}", kvp.Key, kvp.Value));
            }

            if (sbBody.Length > 0)
            {
                sb.Append(sbBody);
            }

            return sb.ToString();
        }

        public abstract void Deserialize(byte[] buffer);

        protected virtual Body[] GetBodies()
        {
            return new Body[0];
        }

        public static SipMessage Parse(byte[] buffer)
        {
            switch (Encoding.Default.GetString(buffer, 0, 3).ToLowerInvariant())
            {
                case "bye":
                    var byeRequest = new ByeRequest();
                    
                    byeRequest.Deserialize(buffer);

                    return byeRequest;
                default:throw new InvalidOperationException();
            }
        }

        protected void ParseRequestLine(string line)
        {
            this.Method = line.Substring(0, 3);
            this.To = line.Substring(3, line.Length -10);
        }

        protected void ParseHeader(string line)
        {
            var headerName = line.Substring(0, line.IndexOf(':'));
            var headerValue = line.Substring(line.IndexOf(':') + 1).TrimStart(' ');
            this.Headers[headerName] = headerValue;
        }
    }
}