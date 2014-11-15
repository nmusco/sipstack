namespace SipStack
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Text;

    public abstract class SipMessage
    {
        private const string BoundaryId = "unique-boundary-1";

        protected SipMessage()
        {
            this.Headers = new OrderedDictionary();
        }

        protected SipMessage(string method)
            : this()
        {
            this.Method = method;
        }

        protected SipMessage(Contact to, string method)
            : this(method)
        {
            this.To = to;

            this.ContactParameters = new List<KeyValuePair<string, string>>();
        }

        public Contact To { get; private set; }

        public List<KeyValuePair<string, string>> ContactParameters { get; private set; }

        public string Method { get; private set; }

        public OrderedDictionary Headers { get; private set; }

        public static SipMessage Parse(byte[] buffer)
        {
            switch (Encoding.Default.GetString(buffer, 0, 3).ToLowerInvariant())
            {
                case "bye":
                    var byeRequest = new ByeRequest();

                    byeRequest.Deserialize(buffer);

                    return byeRequest;
                default: throw new InvalidOperationException();
            }
        }

        public virtual string Serialize()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0} {1}", this.Method, this.To.ToString(false));

            sb.AppendLine(" SIP/2.0");

            var bodies = this.GetBodies();
            if (bodies.Length > 0)
            {
                this.Headers["Content-Type"] = string.Format("multipart/mixed;boundary={0}", BoundaryId);
            }

            var bodyBuilder = new StringBuilder();

            if (bodies.Length > 0)
            {
                bodyBuilder.AppendLine();
                bodyBuilder.AppendLine(string.Format("--{0}", BoundaryId));
                foreach (var b in bodies)
                {
                    bodyBuilder.AppendLine(string.Format("Content-Type: {0}", b.ContentType));
                    foreach (var kvp in b.Headers)
                    {
                        bodyBuilder.AppendLine(string.Format("{0}: {1}", kvp.Key, kvp.Value));
                    }

                    bodyBuilder.Append("\r\n");
                    bodyBuilder.AppendLine(b.ContentText);
                    bodyBuilder.AppendLine(string.Format("--{0}", BoundaryId));
                }

                bodyBuilder.Length = bodyBuilder.Length - 2;
                bodyBuilder.AppendLine("--");
                this.Headers["Content-Length"] = (bodyBuilder.Length - 2).ToString(CultureInfo.InvariantCulture); // content length does not counts on last 2 digits
            }

            foreach (DictionaryEntry kvp in this.Headers)
            {
                sb.AppendLine(string.Format("{0}: {1}", kvp.Key, kvp.Value));
            }

            if (bodyBuilder.Length > 0)
            {
                sb.Append(bodyBuilder);
            }

            return sb.ToString();
        }

        public abstract void Deserialize(byte[] buffer);

        protected virtual Body[] GetBodies()
        {
            return new Body[0];
        }
        
        protected void ParseRequestLine(string line)
        {
            this.Method = line.Substring(0, 3);
            this.To = line.Substring(3, line.Length - 10);
        }

        protected void ParseHeader(string line)
        {
            var headerName = line.Substring(0, line.IndexOf(':'));
            var headerValue = line.Substring(line.IndexOf(':') + 1).TrimStart(' ');
            this.Headers[headerName] = headerValue;
        }
    }
}